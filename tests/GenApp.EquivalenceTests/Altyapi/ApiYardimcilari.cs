using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace GenApp.EquivalenceTests.Altyapi;

/// <summary>
/// İstek gönderme ve cevabı <see cref="JsonDocument"/> ile okuma yardımcıları. API'nin kendi tipleri
/// kullanılmaz; gövdeler sözlük ya da anonim nesneyle kurulur, cevaplar <see cref="JsonElement"/> olarak
/// okunur. Beklenen değerler docs/api-design.md'den gelir; burada yalnızca karşılaştırma yapılır.
/// </summary>
internal static class ApiYardimcilari
{
    public const string MusteriAdresi = "/api/customers";

    public static string MusteriAdresiNumarayla(int musteriNumarasi) => $"{MusteriAdresi}/{musteriNumarasi}";

    /// <summary>
    /// Cevap gövdesini JSON olarak okur. Gövde boşsa ya da JSON değilse (örneğin uç nokta henüz yokken
    /// ASP.NET'in boş gövdeli 404'ü) test, ham gövdeyi gösteren bir mesajla başarısız olur.
    /// </summary>
    public static async Task<JsonElement> JsonOkuAsync(this HttpResponseMessage cevap)
    {
        var metin = await cevap.Content.ReadAsStringAsync();
        try
        {
            using var belge = JsonDocument.Parse(metin);
            return belge.RootElement.Clone();
        }
        catch (JsonException)
        {
            Assert.Fail($"Gövde JSON değil (HTTP {(int)cevap.StatusCode}). Ham gövde: '{metin}'");
            throw; // Assert.Fail her zaman fırlatır; bu satır yalnızca derleyici için.
        }
    }

    /// <summary>HTTP durum kodunu doğrular. Tutmazsa cevap gövdesini de hata mesajına yazar.</summary>
    public static async Task DurumKoduDogrulaAsync(
        this HttpResponseMessage cevap, HttpStatusCode beklenen, string? baglam = null)
    {
        if (cevap.StatusCode != beklenen)
        {
            var govde = await cevap.Content.ReadAsStringAsync();
            var onEk = baglam is null ? "" : $"{baglam}: ";
            Assert.Fail($"{onEk}beklenen HTTP {(int)beklenen}, gelen HTTP {(int)cevap.StatusCode}. Gövde: {govde}");
        }
    }

    /// <summary>
    /// Hata gövdesini (RFC 9457) docs/api-design.md §3'teki eşlemeye göre doğrular: HTTP kodu,
    /// <c>type</c> ve <c>returnCode</c>. <c>status</c>, <c>title</c>, <c>detail</c> ve Content-Type
    /// yalnızca belgedeki örnekte geçtiği için doğrulanmaz.
    /// </summary>
    public static async Task HataGovdesiDogrulaAsync(
        this HttpResponseMessage cevap, HttpStatusCode beklenenHttp, string beklenenType, string beklenenReturnCode)
    {
        await cevap.DurumKoduDogrulaAsync(beklenenHttp);
        var govde = await cevap.JsonOkuAsync();
        NesneDogrula(govde);

        var farklar = new List<string>();
        AlanKarsilastir(govde, "type", beklenenType, farklar);
        AlanKarsilastir(govde, "returnCode", beklenenReturnCode, farklar);

        Assert.True(farklar.Count == 0, FarkMesaji("Hata gövdesi farklı", farklar, govde));
    }

    /// <summary>
    /// Cevaptaki müşteri alanlarını beklenen değerlerle karşılaştırır (alan adları docs/api-design.md §4.1).
    /// Bütün farkları tek mesajda toplar.
    /// </summary>
    public static void MusteriAlanlariDogrula(JsonElement musteri, IReadOnlyDictionary<string, string> beklenen)
    {
        NesneDogrula(musteri);

        var farklar = new List<string>();
        foreach (var (alan, deger) in beklenen)
        {
            AlanKarsilastir(musteri, alan, deger, farklar);
        }

        Assert.True(farklar.Count == 0, FarkMesaji("Müşteri alanları farklı", farklar, musteri));
    }

    /// <summary>
    /// Cevaptaki <c>customerNumber</c> alanını okur. Gövde JSON nesnesi değilse, alan yoksa ya da tam sayı
    /// değilse test, ham gövdeyi gösteren bir mesajla başarısız olur.
    /// </summary>
    public static int MusteriNumarasiOku(JsonElement musteri)
    {
        NesneDogrula(musteri);
        Assert.True(
            musteri.TryGetProperty("customerNumber", out var alan),
            $"Cevapta customerNumber yok. Gövde: {musteri.GetRawText()}");
        Assert.True(
            alan.ValueKind == JsonValueKind.Number && alan.TryGetInt32(out _),
            $"customerNumber tam sayı değil (JSON türü {alan.ValueKind}, değer {alan.GetRawText()}). " +
            $"Gövde: {musteri.GetRawText()}");
        return alan.GetInt32();
    }

    /// <summary>
    /// Testin ön koşulu olan müşteriyi <c>POST /api/customers</c> ile kurar ve numarasını döndürür.
    /// Veritabanı sıfırlanmadığı için her test kendi müşterisini bu yolla kurar.
    /// </summary>
    public static async Task<int> MusteriKurAsync(this HttpClient istemci, IReadOnlyDictionary<string, string> alanlar)
    {
        using var cevap = await istemci.PostAsJsonAsync(MusteriAdresi, alanlar);
        await cevap.DurumKoduDogrulaAsync(HttpStatusCode.Created, "Ön koşul (müşteri ekleme) kurulamadı");
        return MusteriNumarasiOku(await cevap.JsonOkuAsync());
    }

    private static void AlanKarsilastir(JsonElement govde, string alan, string beklenen, List<string> farklar)
    {
        if (!govde.TryGetProperty(alan, out var gelen))
        {
            farklar.Add($"{alan}: cevapta yok");
        }
        else if (gelen.ValueKind != JsonValueKind.String)
        {
            farklar.Add($"{alan}: metin bekleniyordu, gelen JSON türü {gelen.ValueKind} ({gelen.GetRawText()})");
        }
        else if (gelen.GetString() != beklenen)
        {
            farklar.Add($"{alan}: beklenen \"{beklenen}\", gelen {gelen.GetRawText()}");
        }
    }

    /// <summary>
    /// Alan okumadan önce gövdenin JSON nesnesi olduğunu doğrular. Dizi, metin ya da null gelirse
    /// <see cref="JsonElement.TryGetProperty(string, out JsonElement)"/> istisna atacağı için test burada,
    /// ham gövdeyi gösteren bir mesajla başarısız olur.
    /// </summary>
    private static void NesneDogrula(JsonElement govde) =>
        Assert.True(
            govde.ValueKind == JsonValueKind.Object,
            $"Gövde JSON nesnesi değil (JSON türü {govde.ValueKind}). Gövde: {govde.GetRawText()}");

    private static string FarkMesaji(string baslik, List<string> farklar, JsonElement govde) =>
        $"{baslik}:\n  {string.Join("\n  ", farklar)}\nGövde: {govde.GetRawText()}";
}
