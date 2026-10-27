using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace GenApp.EquivalenceTests.Altyapi;

/// <summary>
/// Ticari poliçe testlerinin yardımcıları: adresler (docs/api-design.md §2.5), ön koşul kurma ve liste cevabını okuma.
/// Türden bağımsız okuma ve karşılaştırma <see cref="PoliceYardimcilari"/>'ndadır. API'nin kendi tipleri kullanılmaz;
/// gövdeler sözlükle kurulur, cevaplar <see cref="JsonElement"/> olarak okunur.
/// </summary>
/// <remarks>
/// <see cref="PoliceListesiOku"/>'nun JSON türü kontrolü, <see cref="PoliceYardimcilari"/> ve <see cref="ApiYardimcilari"/>'ndaki
/// private <c>NesneDogrula</c> ile aynı kalıptadır (orada nesne, burada dizi). PoliceYardimcilari bu işte değiştirilmedi;
/// ortak bir yardımcıya taşınması ayrı bir iştir.
/// </remarks>
internal static class TicariYardimcilari
{
    /// <summary>
    /// <c>/api/customers/{customerNumber}/commercial-policies</c> (ekle: POST; sorgula 17c, müşteri numarasıyla liste: GET).
    /// </summary>
    public static string TicariAdresi(int musteriNumarasi) =>
        $"{ApiYardimcilari.MusteriAdresiNumarayla(musteriNumarasi)}/commercial-policies";

    /// <summary>
    /// <c>/api/customers/{customerNumber}/commercial-policies/{policyNumber}</c> (sorgula 17a: GET; sil: DELETE).
    /// </summary>
    public static string TicariAdresiNumarayla(int musteriNumarasi, int policeNumarasi) =>
        $"{TicariAdresi(musteriNumarasi)}/{policeNumarasi}";

    /// <summary><c>/api/commercial-policies/{policyNumber}</c> (sorgula 17b, yalnızca poliçe numarasıyla: GET).</summary>
    public static string PoliceNumarasiylaAdres(int policeNumarasi) => $"/api/commercial-policies/{policeNumarasi}";

    /// <summary><c>/api/commercial-policies?postcode={postcode}</c> (sorgula 17d, posta koduyla liste: GET).</summary>
    public static string PostaKoduylaAdres(string postaKodu) =>
        $"/api/commercial-policies?postcode={Uri.EscapeDataString(postaKodu)}";

    /// <summary>
    /// Testin ön koşulu olan poliçeyi <c>POST /api/customers/{customerNumber}/commercial-policies</c> ile kurar ve
    /// numarasını döndürür. Veritabanı sıfırlanmadığı ve örnek verinin (poliçe 9, 10) yüklü olduğu garanti olmadığı için
    /// her test kendi müşterisini ve poliçesini kurar.
    /// </summary>
    public static async Task<int> TicariPoliceKurAsync(
        this HttpClient istemci, int musteriNumarasi, IReadOnlyDictionary<string, object> alanlar)
    {
        using var cevap = await istemci.PostAsJsonAsync(TicariAdresi(musteriNumarasi), alanlar);
        await cevap.DurumKoduDogrulaAsync(HttpStatusCode.Created, "Ön koşul (ticari poliçe ekleme) kurulamadı");
        return PoliceYardimcilari.PoliceNumarasiOku(await cevap.JsonOkuAsync());
    }

    /// <summary>
    /// Poliçeyi müşteri ve poliçe numarasıyla (17a) <c>GET</c> ile okur ve HTTP 200 bekler. Başka bir kod gelirse test,
    /// <paramref name="baglam"/> ile birlikte cevap gövdesini gösteren bir mesajla başarısız olur.
    /// </summary>
    public static async Task<JsonElement> TicariPoliceOkuAsync(
        this HttpClient istemci, int musteriNumarasi, int policeNumarasi, string baglam)
    {
        using var cevap = await istemci.GetAsync(TicariAdresiNumarayla(musteriNumarasi, policeNumarasi));
        await cevap.DurumKoduDogrulaAsync(HttpStatusCode.OK, baglam);
        return await cevap.JsonOkuAsync();
    }

    /// <summary>
    /// Liste dönen sorguların (17c, 17d) cevabını okur: gövde JSON dizisi olmalıdır (api-design.md §2.5 "poliçe listesi";
    /// §3 kayıt yokken boş liste <c>[]</c>). Dizi değilse test, ham gövdeyi gösteren bir mesajla başarısız olur. Elemanların
    /// sırası doğrulanmaz: belge bir sıra tanımlamaz.
    /// </summary>
    public static IReadOnlyList<JsonElement> PoliceListesiOku(JsonElement govde)
    {
        Assert.True(
            govde.ValueKind == JsonValueKind.Array,
            $"Gövde JSON dizisi değil (JSON türü {govde.ValueKind}). Gövde: {govde.GetRawText()}");
        return govde.EnumerateArray().ToList();
    }
}
