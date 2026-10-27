using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace GenApp.IntegrationTests.Altyapi;

/// <summary>
/// İstek gönderme ve cevabı <see cref="JsonDocument"/> ile okuma yardımcıları. API'nin kendi tipleri kullanılmaz;
/// gövdeler sözlükle kurulur, cevaplar <see cref="JsonElement"/> olarak okunur. Beklenen değerler docs/api-design.md'den
/// (AD) gelir; burada yalnızca karşılaştırma yapılır.
/// </summary>
/// <remarks>
/// <see cref="JsonOkuAsync"/>, <see cref="DurumKoduDogrulaAsync"/>, <see cref="HataGovdesiDogrulaAsync"/> ve onların
/// kullandığı <c>AlanKarsilastir</c>, <c>NesneDogrula</c>, <c>FarkMesaji</c>,
/// <c>tests/GenApp.EquivalenceTests/Altyapi/ApiYardimcilari.cs</c>'ten değiştirilmeden kopyalanmıştır (Aşama 0 karar 5).
/// İki projede aynı kod var; ortak bir projeye taşınması ayrı bir iştir.
/// </remarks>
internal static class ApiYardimcilari
{
    // Adresler: AD §2.

    public const string MusteriAdresi = "/api/customers";

    public static string MusteriAdresiNumarayla(int musteriNumarasi) => $"{MusteriAdresi}/{musteriNumarasi}";

    public static string PoliceAdresi(PoliceTuru tur, int musteriNumarasi) =>
        $"{MusteriAdresiNumarayla(musteriNumarasi)}/{tur.Yol}";

    public static string PoliceAdresi(PoliceTuru tur, int musteriNumarasi, int policeNumarasi) =>
        $"{PoliceAdresi(tur, musteriNumarasi)}/{policeNumarasi}";

    /// <summary>17b: <c>GET /api/commercial-policies/{policyNumber}</c> (AD:72).</summary>
    public static string TicariPoliceAdresi(int policeNumarasi) => $"/api/commercial-policies/{policeNumarasi}";

    /// <summary>17d: <c>GET /api/commercial-policies?postcode={postcode}</c> (AD:74).</summary>
    public static string TicariPostaKoduAdresi(string postaKodu) =>
        $"/api/commercial-policies?postcode={Uri.EscapeDataString(postaKodu)}";

    // Kopyalanan yardımcılar (kaynak: tests/GenApp.EquivalenceTests/Altyapi/ApiYardimcilari.cs).

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

    // Entegrasyon testlerine özgü yardımcılar.

    /// <summary>
    /// Cevaptaki alanları beklenen değerlerle karşılaştırır ve bütün farkları tek mesajda toplar. Beklenen değer
    /// <see cref="string"/> ise JSON metni, <see cref="int"/> ise JSON tam sayısı beklenir (alan türleri AD §4).
    /// </summary>
    public static void AlanlariDogrula(JsonElement govde, IReadOnlyDictionary<string, object> beklenen, string baslik)
    {
        NesneDogrula(govde);

        var farklar = new List<string>();
        foreach (var (alan, deger) in beklenen)
        {
            switch (deger)
            {
                case string metin:
                    AlanKarsilastir(govde, alan, metin, farklar);
                    break;
                case int sayi:
                    SayiKarsilastir(govde, alan, sayi, farklar);
                    break;
                default:
                    throw new ArgumentException($"{alan}: desteklenmeyen beklenen tür {deger.GetType().Name}");
            }
        }

        Assert.True(farklar.Count == 0, FarkMesaji(baslik, farklar, govde));
    }

    /// <summary>
    /// Cevaptaki tam sayı alanını okur (<c>customerNumber</c>, <c>policyNumber</c>; AD §4.1-4.2). Alan yoksa ya da tam
    /// sayı değilse test, ham gövdeyi gösteren bir mesajla başarısız olur.
    /// </summary>
    public static int NumaraOku(JsonElement govde, string alan)
    {
        NesneDogrula(govde);
        Assert.True(govde.TryGetProperty(alan, out var deger), $"Cevapta {alan} yok. Gövde: {govde.GetRawText()}");
        Assert.True(
            deger.ValueKind == JsonValueKind.Number && deger.TryGetInt32(out _),
            $"{alan} tam sayı değil (JSON türü {deger.ValueKind}, değer {deger.GetRawText()}). Gövde: {govde.GetRawText()}");
        return deger.GetInt32();
    }

    /// <summary>
    /// Cevaptaki <c>lastChanged</c> alanını okur. Biçimini sunucu belirler (AD:256-257); testler yalnızca boş olmayan bir
    /// metin olduğunu doğrular ve değeri değiştirmeden geri gönderir.
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
    /// Testin ön koşulu olan müşteriyi <c>POST /api/customers</c> ile kurar (AD:35) ve numarasını döndürür. Veritabanı
    /// sıfırlanmadığı için her test kendi müşterisini bu yolla kurar.
    /// </summary>
    public static async Task<int> MusteriKurAsync(this HttpClient istemci, IReadOnlyDictionary<string, object>? alanlar = null)
    {
        using var cevap = await istemci.PostAsJsonAsync(MusteriAdresi, alanlar ?? TestVerisi.Musteri());
        await cevap.DurumKoduDogrulaAsync(HttpStatusCode.Created, "Ön koşul (müşteri ekleme) kurulamadı");
        return NumaraOku(await cevap.JsonOkuAsync(), "customerNumber");
    }

    /// <summary>
    /// Testin ön koşulu olan poliçeyi <c>POST /api/customers/{c}/{tür}-policies</c> ile kurar (AD §2.2-2.5) ve
    /// poliçe numarasıyla cevap gövdesini döndürür.
    /// </summary>
    public static async Task<(int PoliceNumarasi, JsonElement Govde)> PoliceKurAsync(
        this HttpClient istemci, PoliceTuru tur, int musteriNumarasi, IReadOnlyDictionary<string, object> alanlar)
    {
        using var cevap = await istemci.PostAsJsonAsync(PoliceAdresi(tur, musteriNumarasi), alanlar);
        await cevap.DurumKoduDogrulaAsync(HttpStatusCode.Created, $"Ön koşul ({tur.Yol} ekleme) kurulamadı");
        var govde = await cevap.JsonOkuAsync();
        return (NumaraOku(govde, "policyNumber"), govde);
    }

    /// <summary>
    /// Liste cevabındaki (ticari 17c, 17d; AD:73-74 "poliçe listesi") her öğenin <c>policyNumber</c> değerini okur. Gövde
    /// JSON dizisi değilse test, ham gövdeyi gösteren bir mesajla başarısız olur. Sıra tanımsız olduğu için çağıran
    /// sonucu küme olarak karşılaştırır.
    /// </summary>
    public static IReadOnlyList<int> PoliceNumaralariOku(JsonElement liste)
    {
        Assert.True(
            liste.ValueKind == JsonValueKind.Array,
            $"Gövde JSON dizisi değil (JSON türü {liste.ValueKind}). Gövde: {liste.GetRawText()}");
        return liste.EnumerateArray().Select(oge => NumaraOku(oge, "policyNumber")).ToList();
    }

    /// <summary>
    /// Güncellemenin ön koşulu: poliçeyi sorgular (200 bekler) ve dönen <c>lastChanged</c> değerini döndürür; güncelleme
    /// bu değerle yapılır (AD:23, AD:256).
    /// </summary>
    public static async Task<string> LastChangedSorgulaAsync(
        this HttpClient istemci, PoliceTuru tur, int musteriNumarasi, int policeNumarasi)
    {
        using var cevap = await istemci.GetAsync(PoliceAdresi(tur, musteriNumarasi, policeNumarasi));
        await cevap.DurumKoduDogrulaAsync(HttpStatusCode.OK, $"Ön koşul ({tur.Yol} sorgulama) kurulamadı");
        return LastChangedOku(await cevap.JsonOkuAsync());
    }

    private static void SayiKarsilastir(JsonElement govde, string alan, int beklenen, List<string> farklar)
    {
        if (!govde.TryGetProperty(alan, out var gelen))
        {
            farklar.Add($"{alan}: cevapta yok");
        }
        else if (gelen.ValueKind != JsonValueKind.Number || !gelen.TryGetInt64(out var sayi))
        {
            farklar.Add($"{alan}: tam sayı bekleniyordu, gelen JSON türü {gelen.ValueKind} ({gelen.GetRawText()})");
        }
        else if (sayi != beklenen)
        {
            farklar.Add($"{alan}: beklenen {beklenen}, gelen {gelen.GetRawText()}");
        }
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
