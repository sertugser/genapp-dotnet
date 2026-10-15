using System.Text.Json;

namespace GenApp.EquivalenceTests.Altyapi;

/// <summary>
/// Poliçe testlerinin türden bağımsız yardımcıları: poliçe numarası ve <c>lastChanged</c> okuma, güncelleme gövdesi kurma
/// ve cevaptaki poliçe alanlarını karşılaştırma (docs/api-design.md §4.2, §5). <see cref="ApiYardimcilari"/> gibi API'nin
/// kendi tipleri kullanılmaz; gövdeler sözlükle kurulur, cevaplar <see cref="JsonElement"/> olarak okunur.
/// </summary>
/// <remarks>
/// <see cref="MotorYardimcilari"/>'nda aynı işi yapan kod var (<c>PoliceNumarasiOku</c>, <c>LastChangedOku</c>,
/// <c>GuncellemeGovdesi</c>, alan karşılaştırması). Konut işinde motor dosyalarına dokunulmadı; motorun bu sınıfa
/// geçirilmesi ayrı bir iştir.
/// </remarks>
internal static class PoliceYardimcilari
{
    /// <summary>
    /// Var olmayan müşteri ya da poliçe numarası. Tahminler 0 kullanır; API'nin 0 gibi pozitif olmayan bir numarayı
    /// doğrulamayla 400 ile reddedip reddetmeyeceği api-design.md'de tanımlı değil. Testler o kuralı değil "kayıt yok"
    /// kuralını ölçmeli (ADR 0005 Karar 2, 02-02). Bu yüzden geçerli ama hiç atanmayan bir numara seçildi:
    /// <list type="bullet">
    /// <item>999999999 dokuz hanedir; numaraların taşındığı <c>PIC S9(9) COMP</c> alanlara kesilmeden sığar
    /// (lgapdb01.cbl:90, lgipdb01.cbl:167-168, lgupdb01.cbl:87-88, lgdpdb01.cbl:82-83). COBOL'da da aynı yol çalışır:
    /// satır yok.</item>
    /// <item>Kimlik değerleri 1000001'den başlar (docs/database-schema.md) ve numaralar istekte gönderilse de yok sayılır
    /// (api-design.md §1); bu numaraya testlerde pratikte ulaşılmaz.</item>
    /// </list>
    /// </summary>
    public const int AtanmayanNumara = 999999999;

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
    /// Cevaptaki <c>lastChanged</c> alanını okur. Değerin biçimini sunucu belirler (api-design.md §5; eski sistemde
    /// Db2'nin <c>TIMESTAMP</c> metni); testler yalnızca boş olmayan bir metin olduğunu doğrular ve değeri değiştirmeden
    /// geri gönderir.
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

    /// <summary>
    /// <paramref name="alanlar"/>'ın <paramref name="haric"/> dışındaki alanlarını döndürür: cevapta doğrulanmayan
    /// alanları (örneğin boş gönderilen metinler) beklenen değerlerden çıkarmak için. Sözlükte olmayan bir ad verilirse
    /// istisna atar; yanlış yazılmış bir ad, çıkarılması gereken alanın sessizce doğrulanmasına yol açmasın.
    /// </summary>
    public static IReadOnlyDictionary<string, object> AlanlarHaric(
        IReadOnlyDictionary<string, object> alanlar, IEnumerable<string> haric)
    {
        var cikarilacaklar = haric.ToHashSet();
        var bilinmeyenler = cikarilacaklar.Where(ad => !alanlar.ContainsKey(ad)).ToList();
        if (bilinmeyenler.Count > 0)
        {
            throw new ArgumentException(
                $"Sözlükte olmayan alan: {string.Join(", ", bilinmeyenler)}", nameof(haric));
        }

        return alanlar
            .Where(alan => !cikarilacaklar.Contains(alan.Key))
            .ToDictionary(alan => alan.Key, alan => alan.Value);
    }

    /// <summary>
    /// Cevaptaki poliçe alanlarını beklenen değerlerle karşılaştırır. Beklenen değer <see cref="string"/> ise JSON metin,
    /// <see cref="int"/> ise JSON sayı beklenir. Bütün farkları tek mesajda toplar.
    /// </summary>
    public static void PoliceAlanlariDogrula(JsonElement police, IReadOnlyDictionary<string, object> beklenen)
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
