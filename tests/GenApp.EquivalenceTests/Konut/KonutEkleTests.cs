using System.Net;
using System.Net.Http.Json;
using GenApp.EquivalenceTests.Altyapi;

namespace GenApp.EquivalenceTests.Konut;

/// <summary>
/// Konut poliçesi ekle: <c>POST /api/customers/{customerNumber}/house-policies</c> (eski program LGAPOL01 → LGAPDB01 →
/// LGAPVS01). Tahminler: tests/predictions/08-house-add.md. Beklenen cevaplar: docs/api-design.md §2.3, §3, §4.2, §4.4.
/// 08-01, 08-02 ve 08-03 kendi müşterisini POST ile kurar (musteri-ekle) ve konut-ekle'nin kendisini çağırır; 08-03 ön
/// koşul poliçesini de konut-ekle ile kurar. 08-05 kurulum gerektirmez: var olmayan bir müşteri numarasına POST eder.
/// Bu sınıfın testleri diğer konut operasyonlarına bağlı değildir. Etkinleştirme sırası: konut-ekle, konut-sorgula,
/// konut-guncelle, konut-sil.
/// Test adlarının durum kısmındaki "konut", aynı program ve satıra dayanan motor ekleme testlerinden ayırt etmek içindir.
/// </summary>
public sealed class KonutEkleTests : IClassFixture<GenAppApiFactory>
{
    private readonly HttpClient _istemci;

    public KonutEkleTests(GenAppApiFactory fabrika) => _istemci = fabrika.CreateClient();

    // Tahmin 08-01, lgapdb01.cbl:293 (POLICY INSERT SQLCODE = 0 → CA-RETURN-CODE '00'); ardından HOUSE INSERT ve
    // LGAPVS01 başarılı. Tahmin müşteri 4'ü ve poliçe 6'nın değerlerini kullanır; müşteri POST ile kurulur, poliçe 6'nın
    // değerleri KonutVerisi.Police6'dır. Ön koşullar [B4] ve [B6] yalnızca görülemeyen kısmı (numaranın değeri,
    // CA-LASTCHANGED) etkiler; 00 iki durumda da değişmez (08 "Ön koşullu satırlar").
    // Beklenen: 201 + poliçe (api-design.md §2.3, §3); customerNumber adresteki müşteri, policyNumber tam sayı; alanlar
    // POST edilen değerler: tarihler yyyy-MM-dd (§1), metinler kırpılmış (ADR 0002 Karar 6).
    // Doğrulanmayanlar: numaranın değeri ([B6] ile 1000001; yeni sistemde veritabanı verir), lastChanged (POST cevabında
    // döneceği belgede açık değil; §5 yalnızca sorgulamayı söyler), POLICY.policyType = 'H' (API'de karşılığı yok),
    // VSAM KSDSPOLY kaydı (yeni sistemde VSAM yok), houseName ve brokersReference (boş gönderilir, boş metin beklentisi
    // yazılmaz; KonutVerisi), Location başlığı.
    [Fact]
    [Trait("Operasyon", "konut-ekle")]
    public async Task LGAPDB01_satir_293_konut_police_eklenir_00_doner()
    {
        var musteriNumarasi = await _istemci.MusteriKurAsync(MusteriVerisi.AndrewPandy);

        using var cevap = await _istemci.PostAsJsonAsync(
            KonutYardimcilari.KonutAdresi(musteriNumarasi), KonutVerisi.Police6);

        await cevap.DurumKoduDogrulaAsync(HttpStatusCode.Created);
        var police = await cevap.JsonOkuAsync();
        Assert.Equal(musteriNumarasi, ApiYardimcilari.MusteriNumarasiOku(police));
        PoliceYardimcilari.PoliceNumarasiOku(police);
        PoliceYardimcilari.PoliceAlanlariDogrula(
            police, PoliceYardimcilari.AlanlarHaric(KonutVerisi.Police6, KonutVerisi.BosGonderilenAlanlar));
    }

    // Tahmin 08-02, lgapdb01.cbl:422 (HOUSE INSERT'e CA-H-HOUSE-NAME'in 20 baytı gider; alan X(20), lgcmarea.cpy:60).
    // Girdi: poliçe 6, ama ev adı "Picnic Basket" (db2cre.jcl:543; KonutVerisi.Police6UzunEvAdiyla).
    // Eski sistemde kesilme yalnızca VSAM tarafındaydı: KSDSPOLY kaydında ev adı 9 bayttır (lgapvs01.cbl:46, :122) ve
    // kayda "Picnic Ba" girer. Yeni sistemde VSAM yoktur. Bu test yalnızca POST cevabındaki echo'yu ölçer: cevapta
    // houseName "Picnic Basket" (13 karakter) kesilmeden döner. Değerin veritabanında 20 bayta kadar saklandığını görmek
    // GET gerektirir; konut-ekle testi konut-sorgula'ya bağlanmasın diye GET yapılmaz.
    // Beklenen: 201 (api-design.md §2.3, §3); cevapta houseName "Picnic Basket" (§4.4: en fazla 20).
    // Doğrulanmayanlar: veritabanında saklanan ev adı (GET yok), VSAM kaydı ve 9 baytlık kesilme (yeni sistemde VSAM yok),
    // diğer alanlar (08-01 ölçer), numaranın değeri ([B6]), lastChanged.
    [Fact]
    [Trait("Operasyon", "konut-ekle")]
    public async Task LGAPDB01_satir_422_konut_ev_adi_kesilmez_00_doner()
    {
        var musteriNumarasi = await _istemci.MusteriKurAsync(MusteriVerisi.AndrewPandy);
        var girdi = KonutVerisi.Police6UzunEvAdiyla;

        using var cevap = await _istemci.PostAsJsonAsync(KonutYardimcilari.KonutAdresi(musteriNumarasi), girdi);

        await cevap.DurumKoduDogrulaAsync(HttpStatusCode.Created);
        PoliceYardimcilari.PoliceAlanlariDogrula(
            await cevap.JsonOkuAsync(),
            new Dictionary<string, object> { ["houseName"] = girdi["houseName"] });
    }

    // Tahmin 08-03, lgapdb01.cbl:311 (CA-POLICY-NUM, IDENTITY_VAL_LOCAL()'in verdiği yeni numarayla ezilir, :308-311;
    // INSERT numarayı DEFAULT ile alır, :279). Girdideki numara (tahminde var olan poliçe 6) kullanılmaz.
    // API karşılığı: policyNumber istek gövdesinde gönderilirse yok sayılır, istek reddedilmez (api-design.md §1).
    // Kurulum: müşteri ve poliçe POST ile kurulur; ikinci POST'un gövdesinde ilk poliçenin numarası gönderilir.
    // Beklenen: 201 (§2.3, §3); cevaptaki policyNumber gönderilen numaradan farklı.
    // Doğrulanmayanlar: tahminin CA-LASTCHANGED kısmı (girdideki zaman damgası da yok sayılır, :284, :316-321): POST'ta
    // lastChanged gönderilirse ne olacağı api-design.md'de tanımlı değil (§1 yalnızca customerNumber ve policyNumber'ı
    // sayar), bu yüzden gönderilmez. Var olan poliçenin değişmediği (GET gerektirir; konut-ekle testi konut-sorgula'ya
    // bağlanmaz). Yeni numaranın değeri ([B6]).
    [Fact]
    [Trait("Operasyon", "konut-ekle")]
    public async Task LGAPDB01_satir_311_konut_girdideki_numara_yok_sayilir_00_doner()
    {
        var musteriNumarasi = await _istemci.MusteriKurAsync(MusteriVerisi.AndrewPandy);
        var varOlanPolice = await _istemci.KonutPoliceKurAsync(musteriNumarasi, KonutVerisi.Police6);
        var govde = new Dictionary<string, object>(KonutVerisi.Police6) { ["policyNumber"] = varOlanPolice };

        using var cevap = await _istemci.PostAsJsonAsync(KonutYardimcilari.KonutAdresi(musteriNumarasi), govde);

        await cevap.DurumKoduDogrulaAsync(HttpStatusCode.Created);
        Assert.NotEqual(varOlanPolice, PoliceYardimcilari.PoliceNumarasiOku(await cevap.JsonOkuAsync()));
    }

    // Tahmin 08-05, lgapdb01.cbl:296 (POLICY INSERT yabancı anahtar ihlali, [Db2 bilgisi] SQLCODE = -530 → '70').
    // Müşteri varlığı kodda kontrol edilmez; tek koruma POLICY.customerNumber'ın CUSTOMER'a yabancı anahtarıdır
    // (db2cre.jcl:174-176).
    // Beklenen: 404, type "customer-not-found", returnCode "70" (api-design.md §3, :96: "Poliçe eklenirken müşteri
    // bulunamadı"). Eski sistemin 70'i ile API eşlemesi aynıdır.
    // Numara seçimi: Tahmin müşteri 0 kullanır. PoliceYardimcilari.AtanmayanNumara (999999999): API'nin 0'ı doğrulamayla
    // 400 ile reddedip reddetmeyeceği tanımlı değil; dokuz hanelik numara DB2-CUSTOMERNUM-INT'e (PIC S9(9) COMP,
    // lgapdb01.cbl:90, değer :176'da taşınır) kesilmeden sığar; kimlik değeri 1000001'den başlar ve customerNumber
    // istekte gönderilse de yok sayılır (§1), bu numaraya testlerde pratikte ulaşılmaz. Kurulum gerekmez.
    // Doğrulanmayanlar: HOUSE ve VSAM'a yazılmaması, LGSTSQ hata mesajları (API'den görülmez).
    [Fact]
    [Trait("Operasyon", "konut-ekle")]
    public async Task LGAPDB01_satir_296_konut_musteri_yok_70_doner()
    {
        using var cevap = await _istemci.PostAsJsonAsync(
            KonutYardimcilari.KonutAdresi(PoliceYardimcilari.AtanmayanNumara), KonutVerisi.Police6);

        await cevap.HataGovdesiDogrulaAsync(HttpStatusCode.NotFound, "customer-not-found", "70");
    }
}
