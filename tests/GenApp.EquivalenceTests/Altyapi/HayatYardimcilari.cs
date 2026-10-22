using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace GenApp.EquivalenceTests.Altyapi;

/// <summary>
/// Hayat sigortası poliçesi testlerinin yardımcıları: adresler (docs/api-design.md §2.4) ve ön koşul kurma. Türden
/// bağımsız okuma ve karşılaştırma <see cref="PoliceYardimcilari"/>'ndadır. API'nin kendi tipleri kullanılmaz; gövdeler
/// sözlükle kurulur, cevaplar <see cref="JsonElement"/> olarak okunur.
/// </summary>
internal static class HayatYardimcilari
{
    /// <summary><c>/api/customers/{customerNumber}/endowment-policies</c> (ekle: POST).</summary>
    public static string HayatAdresi(int musteriNumarasi) =>
        $"{ApiYardimcilari.MusteriAdresiNumarayla(musteriNumarasi)}/endowment-policies";

    /// <summary><c>/api/customers/{customerNumber}/endowment-policies/{policyNumber}</c> (sorgula, güncelle, sil).</summary>
    public static string HayatAdresiNumarayla(int musteriNumarasi, int policeNumarasi) =>
        $"{HayatAdresi(musteriNumarasi)}/{policeNumarasi}";

    /// <summary>
    /// Testin ön koşulu olan poliçeyi <c>POST /api/customers/{customerNumber}/endowment-policies</c> ile kurar ve
    /// numarasını döndürür. Veritabanı sıfırlanmadığı ve örnek verinin (poliçe 4, 5) yüklü olduğu garanti olmadığı için her
    /// test kendi müşterisini ve poliçesini kurar.
    /// </summary>
    public static async Task<int> HayatPoliceKurAsync(
        this HttpClient istemci, int musteriNumarasi, IReadOnlyDictionary<string, object> alanlar)
    {
        using var cevap = await istemci.PostAsJsonAsync(HayatAdresi(musteriNumarasi), alanlar);
        await cevap.DurumKoduDogrulaAsync(HttpStatusCode.Created, "Ön koşul (hayat sigortası poliçesi ekleme) kurulamadı");
        return PoliceYardimcilari.PoliceNumarasiOku(await cevap.JsonOkuAsync());
    }

    /// <summary>
    /// Poliçeyi <c>GET</c> ile okur ve HTTP 200 bekler. Başka bir kod gelirse test, <paramref name="baglam"/> ile birlikte
    /// cevap gövdesini gösteren bir mesajla başarısız olur.
    /// </summary>
    public static async Task<JsonElement> HayatPoliceOkuAsync(
        this HttpClient istemci, int musteriNumarasi, int policeNumarasi, string baglam)
    {
        using var cevap = await istemci.GetAsync(HayatAdresiNumarayla(musteriNumarasi, policeNumarasi));
        await cevap.DurumKoduDogrulaAsync(HttpStatusCode.OK, baglam);
        return await cevap.JsonOkuAsync();
    }
}
