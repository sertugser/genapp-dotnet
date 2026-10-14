using System.Net;
using GenApp.EquivalenceTests.Altyapi;

namespace GenApp.EquivalenceTests.Konut;

/// <summary>
/// Konut poliçesi sorgula: <c>GET /api/customers/{customerNumber}/house-policies/{policyNumber}</c> (eski program
/// LGIPOL01 → LGIPDB01). Tahminler: tests/predictions/09-house-inquire.md. Beklenen cevaplar: docs/api-design.md §2.3,
/// §3, §4.2, §4.4.
/// Her test kendi müşterisini POST ile kurar (musteri-ekle). Kurulum için konut poliçesi de POST ile kurulur; bu testler
/// konut-ekle etkin değilken kırmızı olur. Tek istisna 09-05'tir: yalnızca müşteri POST eder. Etkinleştirme sırası:
/// konut-ekle, konut-sorgula, konut-guncelle, konut-sil.
/// </summary>
public sealed class KonutSorgulaTests : IClassFixture<GenAppApiFactory>
{
    private readonly HttpClient _istemci;

    public KonutSorgulaTests(GenAppApiFactory fabrika) => _istemci = fabrika.CreateClient();

    // Tahmin 09-01, lgipdb01.cbl:478 (konut SELECT'i SQLCODE = 0; CA-RETURN-CODE :258'deki '00' olarak kalır, veri
    // :503-504'te COMMAREA'ya taşınır). Poliçe 6'ya güvenilmez: örnek verinin test veritabanına yüklü olduğu garanti
    // değil. Bu yüzden poliçe 6'nın değerleriyle (KonutVerisi.Police6) POST edilen poliçe sorgulanır. Tahmindeki B4 ön
    // koşulu (düzenek SELECT'in host değişkenlerini poliçenin Db2 değerleriyle doldurur) gerçek veritabanında değerlerin
    // satırdan gelmesine karşılık gelir.
    // Beklenen: 200 + poliçe (api-design.md §2.3, §3); policyNumber ve customerNumber adresteki numaralar; alanlar POST
    // edilen değerler: tarihler yyyy-MM-dd (§1), metinler kırpılmış (ADR 0002 Karar 6); lastChanged boş olmayan bir
    // metin (§5: sorgulamanın cevabında döner).
    // Doğrulanmayanlar: houseName ve brokersReference (boş gönderilir, boş metin beklentisi yazılmaz; KonutVerisi),
    // lastChanged'in biçimi (sunucu belirler, §5; eski sistemde de belirsiz: 09 Kod gözlemleri 9), 'FINAL' işareti
    // (:508; COMMAREA'ya ait, API'de karşılığı yok).
    [Fact]
    [Trait("Operasyon", "konut-sorgula")]
    public async Task LGIPDB01_satir_478_police_var_00_doner()
    {
        var musteriNumarasi = await _istemci.MusteriKurAsync(MusteriVerisi.AndrewPandy);
        var policeNumarasi = await _istemci.KonutPoliceKurAsync(musteriNumarasi, KonutVerisi.Police6);

        using var cevap = await _istemci.GetAsync(
            KonutYardimcilari.KonutAdresiNumarayla(musteriNumarasi, policeNumarasi));

        await cevap.DurumKoduDogrulaAsync(HttpStatusCode.OK);
        var police = await cevap.JsonOkuAsync();
        Assert.Equal(policeNumarasi, PoliceYardimcilari.PoliceNumarasiOku(police));
        Assert.Equal(musteriNumarasi, ApiYardimcilari.MusteriNumarasiOku(police));
        PoliceYardimcilari.LastChangedOku(police);
        PoliceYardimcilari.PoliceAlanlariDogrula(
            police, PoliceYardimcilari.AlanlarHaric(KonutVerisi.Police6, KonutVerisi.BosGonderilenAlanlar));
    }

    // Tahmin 09-05, lgipdb01.cbl:514 ([Db2 bilgisi] SELECT satır bulamaz, SQLCODE = 100 (:512) → CA-RETURN-CODE '01').
    // Beklenen: 404, type "not-found", returnCode "01" (api-design.md §3).
    // Kurulum: tahmindeki müşteri 4 gibi var olan bir müşteri POST ile kurulur. Poliçe kurulmaz; bu test konut-ekle'ye
    // bağlı değildir.
    // Numara seçimi: Tahmin 0 kullanır. API'nin 0 gibi pozitif olmayan bir numarayı doğrulamayla 400 ile reddedip
    // reddetmeyeceği api-design.md'de tanımlı değil; bu test o kuralı değil "kayıt yok" kuralını ölçmeli. Bu yüzden
    // geçerli ama hiç atanmayan bir numara seçildi (PoliceYardimcilari.AtanmayanNumara):
    // - 999999999 dokuz hanedir, DB2-POLICYNUM-INT'e (PIC S9(9) COMP, lgipdb01.cbl:168) kesilmeden sığar. COBOL'da da
    //   aynı yol çalışır: satır yok, SQLCODE = 100.
    // - Kimlik değeri 1000001'den başlar (docs/database-schema.md) ve policyNumber istekte gönderilse de yok sayılır
    //   (§1); bu numaraya testlerde pratikte ulaşılmaz.
    // Doğrulanmayanlar: COMMAREA'nın CA-RETURN-CODE dışında değişmemesi ve 'FINAL'in yazılmaması (API'de karşılığı yok).
    [Fact]
    [Trait("Operasyon", "konut-sorgula")]
    public async Task LGIPDB01_satir_514_police_yok_01_doner()
    {
        var musteriNumarasi = await _istemci.MusteriKurAsync(MusteriVerisi.AndrewPandy);

        using var cevap = await _istemci.GetAsync(
            KonutYardimcilari.KonutAdresiNumarayla(musteriNumarasi, PoliceYardimcilari.AtanmayanNumara));

        await cevap.HataGovdesiDogrulaAsync(HttpStatusCode.NotFound, "not-found", "01");
    }

    // Tahmin 09-06, lgipdb01.cbl:514 (WHERE müşteri ve poliçe numarasını birlikte ister, :470-475; [Db2 bilgisi] satır
    // yok, SQLCODE = 100 → '01'). Poliçe numarası tek başına yetmez.
    // Kurulum: A müşterisi ve poliçesi (tahminde müşteri 4 ve poliçe 6) ile B müşterisi (tahminde müşteri 9) POST ile
    // kurulur; A'nın poliçesi B'nin adresinden sorgulanır.
    // Beklenen: 404, type "not-found", returnCode "01" (api-design.md §3).
    // Doğrulanmayanlar: COMMAREA'nın CA-RETURN-CODE dışında değişmemesi (API'de karşılığı yok).
    [Fact]
    [Trait("Operasyon", "konut-sorgula")]
    public async Task LGIPDB01_satir_514_musteri_police_eslesmiyor_01_doner()
    {
        var musteriA = await _istemci.MusteriKurAsync(MusteriVerisi.AndrewPandy);
        var policeNumarasi = await _istemci.KonutPoliceKurAsync(musteriA, KonutVerisi.Police6);
        var musteriB = await _istemci.MusteriKurAsync(MusteriVerisi.AndrewPandy);

        using var cevap = await _istemci.GetAsync(KonutYardimcilari.KonutAdresiNumarayla(musteriB, policeNumarasi));

        await cevap.HataGovdesiDogrulaAsync(HttpStatusCode.NotFound, "not-found", "01");
    }

    // Tahmin 09-07, lgipdb01.cbl:514 (müşteri yok; WHERE (:470-475) satır bulamaz, [Db2 bilgisi] SQLCODE = 100 → '01').
    // Sorgulamada 70 yolu yoktur: '70' lgipol01.cbl'de de lgipdb01.cbl'de de geçmez (09 Kod gözlemleri 6). api-design.md
    // §3'te 70 "Poliçe eklenirken müşteri bulunamadı" (customer-not-found) anlamındadır; sorgulamada müşterinin
    // olmaması da 01 "Kayıt bulunamadı" demektir.
    // Beklenen: 404, type "not-found", returnCode "01" (§3); customer-not-found / 70 değil.
    // Kurulum: tahmindeki poliçe 6 gibi var olan bir poliçe için müşteri ve poliçe POST ile kurulur; sorgu var olmayan
    // müşterinin adresinden yapılır. Tahmin müşteri 0 kullanır; PoliceYardimcilari.AtanmayanNumara (999999999) seçildi
    // (gerekçe 09-05'te; müşteri numarası da DB2-CUSTOMERNUM-INT'e, PIC S9(9) COMP, lgipdb01.cbl:167, sığar).
    // Doğrulanmayanlar: COMMAREA'nın CA-RETURN-CODE dışında değişmemesi (API'de karşılığı yok).
    [Fact]
    [Trait("Operasyon", "konut-sorgula")]
    public async Task LGIPDB01_satir_514_musteri_yok_01_doner()
    {
        var musteriNumarasi = await _istemci.MusteriKurAsync(MusteriVerisi.AndrewPandy);
        var policeNumarasi = await _istemci.KonutPoliceKurAsync(musteriNumarasi, KonutVerisi.Police6);

        using var cevap = await _istemci.GetAsync(
            KonutYardimcilari.KonutAdresiNumarayla(PoliceYardimcilari.AtanmayanNumara, policeNumarasi));

        await cevap.HataGovdesiDogrulaAsync(HttpStatusCode.NotFound, "not-found", "01");
    }
}
