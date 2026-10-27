using System.Text.RegularExpressions;
using Npgsql;

namespace GenApp.IntegrationTests.Altyapi;

/// <summary>
/// Veritabanını uygulamadan bağımsız bir yoldan, Npgsql ile ham SQL çalıştırarak okur. Uygulamanın DbContext'i,
/// repository'si ya da entity'leri kullanılmaz. Tablo ve sütun adları <see cref="Sema"/>'dan gelir; değerler her zaman
/// parametreyle gönderilir.
/// </summary>
/// <remarks>
/// Veritabanı testler arasında sıfırlanmaz ve örnek veri içerebilir. Bu yüzden sorgular yalnızca testin kendi kurduğu
/// anahtarlarla (müşteri numarası N, poliçe numarası P, posta kodu Z) yapılır; "tam N satır" beklentileri bu anahtarlara
/// bağlıdır.
/// </remarks>
public sealed partial class VeritabaniYardimcilari(NpgsqlDataSource veriKaynagi)
{
    /// <summary><c>anahtarSutun = anahtar</c> koşuluna uyan satırları sütun adı → değer sözlüğü olarak okur.</summary>
    public async Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> SatirlariOkuAsync(
        string tablo, string anahtarSutun, object anahtar)
    {
        await using var komut = veriKaynagi.CreateCommand(
            $"SELECT * FROM {Ad(tablo)} WHERE {Ad(anahtarSutun)} = @anahtar");
        komut.Parameters.AddWithValue("anahtar", anahtar);

        await using var okuyucu = await komut.ExecuteReaderAsync();
        var satirlar = new List<IReadOnlyDictionary<string, object?>>();
        while (await okuyucu.ReadAsync())
        {
            var satir = new Dictionary<string, object?>();
            for (var i = 0; i < okuyucu.FieldCount; i++)
            {
                satir[okuyucu.GetName(i)] = okuyucu.IsDBNull(i) ? null : okuyucu.GetValue(i);
            }
            satirlar.Add(satir);
        }
        return satirlar;
    }

    /// <summary>Koşula uyan tam 1 satır olduğunu doğrular ve onu döndürür.</summary>
    public async Task<IReadOnlyDictionary<string, object?>> TekSatirOkuAsync(
        string tablo, string anahtarSutun, object anahtar)
    {
        var satirlar = await SatirlariOkuAsync(tablo, anahtarSutun, anahtar);
        Assert.True(
            satirlar.Count == 1,
            $"{tablo} tablosunda {anahtarSutun} = {anahtar} için tam 1 satır bekleniyordu, {satirlar.Count} satır var.");
        return satirlar[0];
    }

    /// <summary>Bütün koşullara (<c>sütun = değer</c>, AND ile) uyan satırları sayar.</summary>
    public async Task<long> SayAsync(string tablo, params (string Sutun, object Deger)[] kosullar)
    {
        Assert.True(kosullar.Length > 0, "SayAsync en az bir koşulla çağrılmalı (veritabanı paylaşılıyor).");

        var where = string.Join(" AND ", kosullar.Select((k, i) => $"{Ad(k.Sutun)} = @p{i}"));
        await using var komut = veriKaynagi.CreateCommand($"SELECT count(*) FROM {Ad(tablo)} WHERE {where}");
        for (var i = 0; i < kosullar.Length; i++)
        {
            komut.Parameters.AddWithValue($"p{i}", kosullar[i].Deger);
        }
        return (long)(await komut.ExecuteScalarAsync())!;
    }

    /// <summary><c>policy.last_changed</c> değerini okur (tam 1 satır beklenir). Değer metin olarak karşılaştırılmaz.</summary>
    public async Task<DateTime?> LastChangedOkuAsync(int policeNumarasi)
    {
        var satir = await TekSatirOkuAsync(Sema.Policy.Tablo, Sema.Policy.PolicyNumber, policeNumarasi);
        return satir[Sema.Policy.LastChanged] as DateTime?;
    }

    /// <summary>Bağlantının kullandığı şemadaki (<c>current_schema()</c>) tabloların adları.</summary>
    public async Task<IReadOnlySet<string>> TablolariOkuAsync()
    {
        await using var komut = veriKaynagi.CreateCommand(
            "SELECT table_name FROM information_schema.tables " +
            "WHERE table_schema = current_schema() AND table_type = 'BASE TABLE'");
        await using var okuyucu = await komut.ExecuteReaderAsync();
        var tablolar = new HashSet<string>();
        while (await okuyucu.ReadAsync())
        {
            tablolar.Add(okuyucu.GetString(0));
        }
        return tablolar;
    }

    /// <summary>Bağlantının kullandığı şemadaki tabloların sütunları (tablo → sütun adları).</summary>
    public async Task<IReadOnlyDictionary<string, IReadOnlySet<string>>> SutunlariOkuAsync()
    {
        await using var komut = veriKaynagi.CreateCommand(
            "SELECT table_name, column_name FROM information_schema.columns WHERE table_schema = current_schema()");
        await using var okuyucu = await komut.ExecuteReaderAsync();
        var sutunlar = new Dictionary<string, HashSet<string>>();
        while (await okuyucu.ReadAsync())
        {
            var tablo = okuyucu.GetString(0);
            if (!sutunlar.TryGetValue(tablo, out var kume))
            {
                sutunlar[tablo] = kume = [];
            }
            kume.Add(okuyucu.GetString(1));
        }
        return sutunlar.ToDictionary(k => k.Key, k => (IReadOnlySet<string>)k.Value);
    }

    /// <summary>
    /// Satırdaki sütunları beklenen değerlerle karşılaştırır ve bütün farkları tek mesajda toplar. Beklenen değerler:
    /// metin için <see cref="string"/>, tam sayı için <see cref="int"/>, tarih için <see cref="DateOnly"/>. Npgsql'in
    /// döndürdüğü <c>smallint</c>/<c>integer</c> ve <c>date</c> değerleri bu türlere çevrilerek karşılaştırılır.
    /// </summary>
    public static void SatirDogrula(
        IReadOnlyDictionary<string, object?> satir, IReadOnlyDictionary<string, object> beklenen, string tablo)
    {
        var farklar = new List<string>();
        foreach (var (sutun, beklenenDeger) in beklenen)
        {
            if (!satir.TryGetValue(sutun, out var gelen))
            {
                farklar.Add($"{sutun}: satırda böyle bir sütun yok");
            }
            else if (!Equals(Normallestir(gelen), Normallestir(beklenenDeger)))
            {
                farklar.Add($"{sutun}: beklenen {Goster(beklenenDeger)}, gelen {Goster(gelen)}");
            }
        }

        var satirMetni = string.Join(", ", satir.Select(s => $"{s.Key}={Goster(s.Value)}"));
        Assert.True(
            farklar.Count == 0,
            $"{tablo} satırı farklı:\n  {string.Join("\n  ", farklar)}\nSatır: {satirMetni}");
    }

    private static object? Normallestir(object? deger) => deger switch
    {
        short s => (long)s,
        int i => (long)i,
        long l => l,
        // date sütunu Npgsql'den gece yarısı DateTime olarak gelir.
        DateTime { TimeOfDay.Ticks: 0 } t => DateOnly.FromDateTime(t),
        _ => deger,
    };

    private static string Goster(object? deger) => deger switch
    {
        null => "NULL",
        string s => $"\"{s}\"",
        DateOnly d => d.ToString("yyyy-MM-dd"),
        _ => deger.ToString() ?? "",
    };

    /// <summary>Tablo ve sütun adları SQL metnine girdiği için yalnızca küçük harf, rakam ve alt çizgiye izin verilir.</summary>
    private static string Ad(string ad) =>
        GuvenliAd().IsMatch(ad) ? ad : throw new ArgumentException($"Geçersiz tablo ya da sütun adı: '{ad}'", nameof(ad));

    [GeneratedRegex("^[a-z_][a-z0-9_]*$")]
    private static partial Regex GuvenliAd();
}
