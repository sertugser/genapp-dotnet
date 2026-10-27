using System.Net;
using System.Net.Http.Json;
using GenApp.EquivalenceTests.Altyapi;

namespace GenApp.EquivalenceTests.Ticari;

/// <summary>
/// Ticari poliçe ekle: <c>POST /api/customers/{customerNumber}/commercial-policies</c> (eski program LGAPOL01 → LGAPDB01 →
/// LGAPVS01). Tahminler: tests/predictions/16-commercial-add.md. Beklenen cevaplar: docs/api-design.md §2.5, §3, §4.2, §4.6.
/// 16-01 ve 16-02 kendi müşterisini POST ile kurar (musteri-ekle) ve ticari-ekle'nin kendisini çağırır. 16-03 hiçbir kurulum
/// yapmaz: var olmayan bir müşteri numarasına POST eder. Bu sınıfın testleri diğer ticari operasyonlara bağlı değildir.
/// Etkinleştirme sırası: musteri-ekle, ticari-ekle, ticari-sorgula, ticari-sil.
/// 16-01 ve 16-03'ün adlarındaki "ticari", aynı program ve satıra dayanan motor ekleme testlerinden ayırt etmek içindir.
/// </summary>
public sealed class TicariEkleTests : IClassFixture<GenAppApiFactory>
{
    private readonly HttpClient _istemci;

    public TicariEkleTests(GenAppApiFactory fabrika) => _istemci = fabrika.CreateClient();

    // Tahmin 16-01, lgapdb01.cbl:293 (POLICY INSERT SQLCODE = 0 → CA-RETURN-CODE '00'); ardından COMMERCIAL INSERT ve LGAPVS01
    // başarılı. Tahmin müşteri 1'i ve poliçe 10 ile COMMERCIAL 10'un değerlerini kullanır; müşteri POST ile kurulur, poliçenin
    // değerleri TicariVerisi.Police10'dur (tarihler POLICY 10'dan; bkz. TicariVerisi). Ön koşullar [B4] ve [B6] yalnızca
    // görülemeyen kısmı (numaranın değeri, CA-LASTCHANGED) etkiler; 00 iki durumda da değişmez (16 "Ön koşullu satırlar").
    // Beklenen: 201 + poliçe (api-design.md §2.5 :70, §3); customerNumber adresteki müşteri, policyNumber tam sayı; alanlar
    // POST edilen değerler: tarihler yyyy-MM-dd (§1), metinler kırpılmış (ADR 0002 Karar 6).
    // Doğrulanmayanlar: numaranın değeri ([B6] ile 1000001; yeni sistemde veritabanı verir), lastChanged (POST cevabında
    // döneceği belgede açık değil; eski sistemde COMMERCIAL.RequestDate = yeni POLICY.lastChanged, 16 Kod gözlemleri 7),
    // POLICY.policyType = 'C' (API'de karşılığı yok), VSAM KSDSPOLY kaydı (yeni sistemde VSAM yok), brokersReference (boş
    // gönderilir, boş metin beklentisi yazılmaz; TicariVerisi), Location başlığı.
    [Fact]
    [Trait("Operasyon", "ticari-ekle")]
    public async Task LGAPDB01_satir_293_ticari_police_eklenir_00_doner()
    {
        var musteriNumarasi = await _istemci.MusteriKurAsync(MusteriVerisi.AndrewPandy);

        using var cevap = await _istemci.PostAsJsonAsync(
            TicariYardimcilari.TicariAdresi(musteriNumarasi), TicariVerisi.Police10);

        await cevap.DurumKoduDogrulaAsync(HttpStatusCode.Created);
        var police = await cevap.JsonOkuAsync();
        Assert.Equal(musteriNumarasi, ApiYardimcilari.MusteriNumarasiOku(police));
        PoliceYardimcilari.PoliceNumarasiOku(police);
        PoliceYardimcilari.PoliceAlanlariDogrula(
            police, PoliceYardimcilari.AlanlarHaric(TicariVerisi.Police10, TicariVerisi.BosGonderilenAlanlar));
    }

    // Tahmin 16-02, lgapdb01.cbl:532 (COMMERCIAL INSERT'e CA-B-Customer'ın 255 baytı gider; alan X(255), lgcmarea.cpy:82).
    // Girdi: poliçe 10, ama customer 39 karakterlik "Burnley Football Club TURF MOOR STADIUM" (parçaları db2cre.jcl:1235,
    // :1231, :1236; TicariVerisi.Police10UzunIsletmeAdiyla).
    // Eski sistemde kesilme yalnızca VSAM tarafındaydı: KSDSPOLY kaydında müşteri metni 31 bayttır (lgapvs01.cbl:34, :108) ve
    // kayda "Burnley Football Club TURF MOOR" girer; Db2'deki COMMERCIAL.Customer metnin tamamıdır. Yeni sistemde VSAM yoktur.
    // Bu test yalnızca POST cevabındaki echo'yu ölçer: cevapta customer 39 karakterin tamamı döner. Değerin veritabanında
    // saklandığını görmek GET gerektirir; ticari-ekle testi ticari-sorgula'ya bağlanmasın diye GET yapılmaz.
    // Beklenen: 201 (api-design.md §2.5 :70, §3); cevapta customer "Burnley Football Club TURF MOOR STADIUM" (§4.6: en fazla
    // 255).
    // Doğrulanmayanlar: veritabanında saklanan metin (GET yok), VSAM kaydı ve 31 baytlık kesilme (yeni sistemde VSAM yok),
    // diğer alanlar (16-01 ölçer), numaranın değeri ([B6]), lastChanged.
    [Fact]
    [Trait("Operasyon", "ticari-ekle")]
    public async Task LGAPDB01_satir_532_isletme_adi_kesilmez_00_doner()
    {
        var musteriNumarasi = await _istemci.MusteriKurAsync(MusteriVerisi.AndrewPandy);
        var girdi = TicariVerisi.Police10UzunIsletmeAdiyla;

        using var cevap = await _istemci.PostAsJsonAsync(TicariYardimcilari.TicariAdresi(musteriNumarasi), girdi);

        await cevap.DurumKoduDogrulaAsync(HttpStatusCode.Created);
        PoliceYardimcilari.PoliceAlanlariDogrula(
            await cevap.JsonOkuAsync(),
            new Dictionary<string, object> { ["customer"] = girdi["customer"] });
    }

    // Tahmin 16-03, lgapdb01.cbl:296 (POLICY INSERT yabancı anahtar ihlali, [Db2 bilgisi] SQLCODE = -530 → '70').
    // Müşteri varlığı kodda kontrol edilmez; tek koruma POLICY.customerNumber'ın CUSTOMER'a yabancı anahtarıdır
    // (db2cre.jcl:174-176).
    // Beklenen: 404, type "customer-not-found", returnCode "70" (api-design.md §3, :96: "Poliçe eklenirken müşteri
    // bulunamadı"). Eski sistemin 70'i ile API eşlemesi aynıdır.
    // Numara seçimi: Tahmin müşteri 0 kullanır. PoliceYardimcilari.AtanmayanNumara (999999999): API'nin 0'ı doğrulamayla
    // 400 ile reddedip reddetmeyeceği tanımlı değil; dokuz hanelik numara DB2-CUSTOMERNUM-INT'e (PIC S9(9) COMP,
    // lgapdb01.cbl:90, değer :176'da taşınır) kesilmeden sığar; kimlik değeri 1000001'den başlar ve customerNumber
    // istekte gönderilse de yok sayılır (§1), bu numaraya testlerde pratikte ulaşılmaz. Kurulum gerekmez.
    // Doğrulanmayanlar: COMMERCIAL ve VSAM'a yazılmaması, LGSTSQ hata mesajları (API'den görülmez).
    [Fact]
    [Trait("Operasyon", "ticari-ekle")]
    public async Task LGAPDB01_satir_296_ticari_musteri_yok_70_doner()
    {
        using var cevap = await _istemci.PostAsJsonAsync(
            TicariYardimcilari.TicariAdresi(PoliceYardimcilari.AtanmayanNumara), TicariVerisi.Police10);

        await cevap.HataGovdesiDogrulaAsync(HttpStatusCode.NotFound, "customer-not-found", "70");
    }
}
