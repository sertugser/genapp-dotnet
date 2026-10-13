using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace GenApp.EquivalenceTests.Altyapi;

/// <summary>
/// Motor poliçesi testlerinin yardımcıları: adresler (docs/api-design.md §2.2), ön koşul kurma ve cevap
/// alanlarını karşılaştırma (§4.2, §4.3). <see cref="ApiYardimcilari"/> gibi API'nin kendi tipleri kullanılmaz;
/// gövdeler sözlükle kurulur, cevaplar <see cref="JsonElement"/> olarak okunur.
/// </summary>
internal static class MotorYardimcilari
{
    /// <summary><c>/api/customers/{customerNumber}/motor-policies</c> (ekle: POST).</summary>
    public static string MotorAdresi(int musteriNumarasi) =>
        $"{ApiYardimcilari.MusteriAdresiNumarayla(musteriNumarasi)}/motor-policies";

    /// <summary><c>/api/customers/{customerNumber}/motor-policies/{policyNumber}</c> (sorgula, güncelle, sil).</summary>
    public static string MotorAdresiNumarayla(int musteriNumarasi, int policeNumarasi) =>
        $"{MotorAdresi(musteriNumarasi)}/{policeNumarasi}";

    /// <summary>
    /// Testin ön koşulu olan poliçeyi <c>POST /api/customers/{customerNumber}/motor-policies</c> ile kurar ve
    /// numarasını döndürür. Veritabanı sıfırlanmadığı için her test kendi müşterisini ve poliçesini kurar.
    /// </summary>
    public static async Task<int> MotorPoliceKurAsync(
        this HttpClient istemci, int musteriNumarasi, IReadOnlyDictionary<string, object> alanlar)
    {
        using var cevap = await istemci.PostAsJsonAsync(MotorAdresi(musteriNumarasi), alanlar);
        await cevap.DurumKoduDogrulaAsync(HttpStatusCode.Created, "Ön koşul (motor poliçesi ekleme) kurulamadı");
        return PoliceNumarasiOku(await cevap.JsonOkuAsync());
    }

    /// <summary>
    /// Poliçeyi <c>GET</c> ile okur ve HTTP 200 bekler. Başka bir kod gelirse test, <paramref name="baglam"/> ile
    /// birlikte cevap gövdesini gösteren bir mesajla başarısız olur.
    /// </summary>
    public static async Task<JsonElement> MotorPoliceOkuAsync(
        this HttpClient istemci, int musteriNumarasi, int policeNumarasi, string baglam)
    {
        using var cevap = await istemci.GetAsync(MotorAdresiNumarayla(musteriNumarasi, policeNumarasi));
        await cevap.DurumKoduDogrulaAsync(HttpStatusCode.OK, baglam);
        return await cevap.JsonOkuAsync();
    }

    /// <summary>
    /// Güncelleme isteğinin gövdesini kurar: poliçe alanları ve sorgulamadan alınan <c>lastChanged</c>
    /// (api-design.md §5: değer aynen geri gönderilir).
    /// </summary>
    public static Dictionary<string, object> GuncellemeGovdesi(
        IReadOnlyDictionary<string, object> alanlar, string lastChanged)
    {
        var govde = alanlar.ToDictionary(alan => alan.Key, alan => alan.Value);
        govde["lastChanged"] = lastChanged;
        return govde;
    }

    /// <summary>Cevaptaki <c>policyNumber</c> alanını okur (tam sayı değilse test, ham gövdeyle başarısız olur).</summary>
    public static int PoliceNumarasiOku(JsonElement police)
    {
        NesneDogrula(police);
        Assert.True(
            police.TryGetProperty("policyNumber", out var alan),
            $"Cevapta policyNumber yok. Gövde: {police.GetRawText()}");
        Assert.True(
            alan.ValueKind == JsonValueKind.Number && alan.TryGetInt32(out _),
            $"policyNumber tam sayı değil (JSON türü {alan.ValueKind}, değer {alan.GetRawText()}). " +
            $"Gövde: {police.GetRawText()}");
        return alan.GetInt32();
    }

    /// <summary>
    /// Cevaptaki <c>lastChanged</c> alanını okur. Değerin biçimini sunucu belirler (api-design.md §5; eski
    /// sistemde Db2'nin <c>TIMESTAMP</c> metni); testler yalnızca boş olmayan bir metin olduğunu doğrular ve
    /// değeri değiştirmeden geri gönderir.
    /// </summary>
    public static string LastChangedOku(JsonElement police)
    {
        NesneDogrula(police);
        Assert.True(
            police.TryGetProperty("lastChanged", out var alan),
            $"Cevapta lastChanged yok. Gövde: {police.GetRawText()}");
        Assert.True(
            alan.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(alan.GetString()),
            $"lastChanged boş olmayan bir metin değil (JSON türü {alan.ValueKind}, değer {alan.GetRawText()}). " +
            $"Gövde: {police.GetRawText()}");
        return alan.GetString()!;
    }

    /// <summary>
    /// Cevaptaki poliçe alanlarını beklenen değerlerle karşılaştırır. Beklenen değer <see cref="string"/> ise JSON
    /// metin, <see cref="int"/> ise JSON sayı beklenir. Bütün farkları tek mesajda toplar.
    /// </summary>
    public static void MotorAlanlariDogrula(JsonElement police, IReadOnlyDictionary<string, object> beklenen)
    {
        NesneDogrula(police);

        var farklar = new List<string>();
        foreach (var (alan, deger) in beklenen)
        {
            AlanKarsilastir(police, alan, deger, farklar);
        }

        Assert.True(
            farklar.Count == 0,
            $"Poliçe alanları farklı:\n  {string.Join("\n  ", farklar)}\nGövde: {police.GetRawText()}");
    }

    private static void AlanKarsilastir(JsonElement police, string alan, object beklenen, List<string> farklar)
    {
        if (!police.TryGetProperty(alan, out var gelen))
        {
            farklar.Add($"{alan}: cevapta yok");
            return;
        }

        switch (beklenen)
        {
            case string metin:
                if (gelen.ValueKind != JsonValueKind.String)
                {
                    farklar.Add($"{alan}: metin bekleniyordu, gelen JSON türü {gelen.ValueKind} ({gelen.GetRawText()})");
                }
                else if (gelen.GetString() != metin)
                {
                    farklar.Add($"{alan}: beklenen \"{metin}\", gelen {gelen.GetRawText()}");
                }
                break;

            case int sayi:
                if (gelen.ValueKind != JsonValueKind.Number || !gelen.TryGetInt32(out var gelenSayi))
                {
                    farklar.Add($"{alan}: tam sayı bekleniyordu, gelen JSON türü {gelen.ValueKind} ({gelen.GetRawText()})");
                }
                else if (gelenSayi != sayi)
                {
                    farklar.Add($"{alan}: beklenen {sayi}, gelen {gelen.GetRawText()}");
                }
                break;

            default:
                farklar.Add($"{alan}: testin beklenen değer türü desteklenmiyor ({beklenen.GetType().Name})");
                break;
        }
    }

    /// <summary>
    /// Alan okumadan önce gövdenin JSON nesnesi olduğunu doğrular; dizi, metin ya da null gelirse
    /// <see cref="JsonElement.TryGetProperty(string, out JsonElement)"/> istisna atacağı için test burada,
    /// ham gövdeyi gösteren bir mesajla başarısız olur.
    /// </summary>
    private static void NesneDogrula(JsonElement govde) =>
        Assert.True(
            govde.ValueKind == JsonValueKind.Object,
            $"Gövde JSON nesnesi değil (JSON türü {govde.ValueKind}). Gövde: {govde.GetRawText()}");
}
