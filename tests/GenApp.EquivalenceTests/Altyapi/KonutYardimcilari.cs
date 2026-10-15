using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace GenApp.EquivalenceTests.Altyapi;

/// <summary>
/// Konut poliçesi testlerinin yardımcıları: adresler (docs/api-design.md §2.3) ve ön koşul kurma. Türden bağımsız okuma
/// ve karşılaştırma <see cref="PoliceYardimcilari"/>'ndadır. API'nin kendi tipleri kullanılmaz; gövdeler sözlükle
/// kurulur, cevaplar <see cref="JsonElement"/> olarak okunur.
/// </summary>
internal static class KonutYardimcilari
{
    /// <summary><c>/api/customers/{customerNumber}/house-policies</c> (ekle: POST).</summary>
    public static string KonutAdresi(int musteriNumarasi) =>
        $"{ApiYardimcilari.MusteriAdresiNumarayla(musteriNumarasi)}/house-policies";

    /// <summary><c>/api/customers/{customerNumber}/house-policies/{policyNumber}</c> (sorgula, güncelle, sil).</summary>
    public static string KonutAdresiNumarayla(int musteriNumarasi, int policeNumarasi) =>
        $"{KonutAdresi(musteriNumarasi)}/{policeNumarasi}";

    /// <summary>
    /// Testin ön koşulu olan poliçeyi <c>POST /api/customers/{customerNumber}/house-policies</c> ile kurar ve numarasını
    /// döndürür. Veritabanı sıfırlanmadığı ve örnek verinin (poliçe 6, 7, 8) yüklü olduğu garanti olmadığı için her test
    /// kendi müşterisini ve poliçesini kurar.
    /// </summary>
    public static async Task<int> KonutPoliceKurAsync(
        this HttpClient istemci, int musteriNumarasi, IReadOnlyDictionary<string, object> alanlar)
    {
        using var cevap = await istemci.PostAsJsonAsync(KonutAdresi(musteriNumarasi), alanlar);
        await cevap.DurumKoduDogrulaAsync(HttpStatusCode.Created, "Ön koşul (konut poliçesi ekleme) kurulamadı");
        return PoliceYardimcilari.PoliceNumarasiOku(await cevap.JsonOkuAsync());
    }

    /// <summary>
    /// Poliçeyi <c>GET</c> ile okur ve HTTP 200 bekler. Başka bir kod gelirse test, <paramref name="baglam"/> ile birlikte
    /// cevap gövdesini gösteren bir mesajla başarısız olur.
    /// </summary>
    public static async Task<JsonElement> KonutPoliceOkuAsync(
        this HttpClient istemci, int musteriNumarasi, int policeNumarasi, string baglam)
    {
        using var cevap = await istemci.GetAsync(KonutAdresiNumarayla(musteriNumarasi, policeNumarasi));
        await cevap.DurumKoduDogrulaAsync(HttpStatusCode.OK, baglam);
        return await cevap.JsonOkuAsync();
    }
}
