using System.Net;
using System.Net.Http.Json;
using GenApp.EquivalenceTests.Altyapi;

namespace GenApp.EquivalenceTests.Hayat;

/// <summary>
/// Hayat sigortası poliçesi ekle: <c>POST /api/customers/{customerNumber}/endowment-policies</c> (eski program LGAPOL01 →
/// LGAPDB01 → LGAPVS01). Tahminler: tests/predictions/12-endowment-add.md. Beklenen cevaplar: docs/api-design.md §2.4, §3,
/// §4.2, §4.5.
/// 12-01, 12-21 ve 12-27 kendi müşterisini POST ile kurar (musteri-ekle) ve hayat-ekle'nin kendisini çağırır; 12-21 ön
/// koşul poliçesini de hayat-ekle ile kurar. 12-15 kurulum gerektirmez: var olmayan bir müşteri numarasına POST eder.
/// Bu sınıfın testleri diğer hayat operasyonlarına bağlı değildir. Etkinleştirme sırası: hayat-ekle, hayat-sorgula,
/// hayat-guncelle, hayat-sil.
/// Test adlarının durum kısmındaki "hayat", aynı program ve satıra dayanan motor ve konut ekleme testlerinden ayırt etmek
/// içindir.
/// </summary>
public sealed class HayatEkleTests : IClassFixture<GenAppApiFactory>
{
    private readonly HttpClient _istemci;

    public HayatEkleTests(GenAppApiFactory fabrika) => _istemci = fabrika.CreateClient();

    // Tahmin 12-01, lgapdb01.cbl:293 (POLICY INSERT SQLCODE = 0 → CA-RETURN-CODE '00'); ardından ENDOWMENT INSERT ve
    // LGAPVS01 başarılı. Tahmin müşteri 8'i ve poliçe 4'ün değerlerini kullanır; müşteri POST ile kurulur, poliçe 4'ün
    // değerleri HayatVerisi.Police4'tür. Ön koşullar [B3], [B4] ve [B6] yalnızca görülemeyen kısmı (numaranın değeri,
    // CA-LASTCHANGED, dolgu uzunluğu) etkiler; 00 değişmez (12 "Ön koşullu satırlar").
    // Beklenen: 201 + poliçe (api-design.md §2.4, §3); customerNumber adresteki müşteri, policyNumber tam sayı; alanlar
    // POST edilen değerler: tarihler yyyy-MM-dd (§1), metinler kırpılmış (ADR 0002 Karar 6).
    // Doğrulanmayanlar: numaranın değeri ([B6] ile 1000001; yeni sistemde veritabanı verir), lastChanged (POST cevabında
    // döneceği belgede açık değil; §5 yalnızca sorgulamayı söyler), POLICY.policyType = 'E' (API'de karşılığı yok),
    // ENDOWMENT.paddingData (API'de yok, §4.5), VSAM KSDSPOLY kaydı (yeni sistemde VSAM yok), brokersReference (boş
    // gönderilir, boş metin beklentisi yazılmaz; HayatVerisi), Location başlığı.
    [Fact]
    [Trait("Operasyon", "hayat-ekle")]
    public async Task LGAPDB01_satir_293_hayat_police_eklenir_00_doner()
    {
        var musteriNumarasi = await _istemci.MusteriKurAsync(MusteriVerisi.AndrewPandy);

        using var cevap = await _istemci.PostAsJsonAsync(
            HayatYardimcilari.HayatAdresi(musteriNumarasi), HayatVerisi.Police4);

        await cevap.DurumKoduDogrulaAsync(HttpStatusCode.Created);
        var police = await cevap.JsonOkuAsync();
        Assert.Equal(musteriNumarasi, ApiYardimcilari.MusteriNumarasiOku(police));
        PoliceYardimcilari.PoliceNumarasiOku(police);
        PoliceYardimcilari.PoliceAlanlariDogrula(
            police, PoliceYardimcilari.AlanlarHaric(HayatVerisi.Police4, HayatVerisi.BosGonderilenAlanlar));
    }

    // Tahmin 12-15, lgapdb01.cbl:296 (POLICY INSERT yabancı anahtar ihlali, [Db2 bilgisi] SQLCODE = -530 → '70').
    // Müşteri varlığı kodda kontrol edilmez; tek koruma POLICY.customerNumber'ın CUSTOMER'a yabancı anahtarıdır
    // (db2cre.jcl:174-176).
    // Beklenen: 404, type "customer-not-found", returnCode "70" (api-design.md §3: "Poliçe eklenirken müşteri
    // bulunamadı"). Eski sistemin 70'i ile API eşlemesi aynıdır.
    // Numara seçimi: Tahmin müşteri 0 kullanır. PoliceYardimcilari.AtanmayanNumara (999999999): API'nin 0'ı doğrulamayla
    // 400 ile reddedip reddetmeyeceği tanımlı değil; dokuz hanelik numara DB2-CUSTOMERNUM-INT'e (PIC S9(9) COMP,
    // lgapdb01.cbl:90, değer :176'da taşınır) kesilmeden sığar; kimlik değeri 1000001'den başlar ve customerNumber
    // istekte gönderilse de yok sayılır (§1), bu numaraya testlerde pratikte ulaşılmaz. Kurulum gerekmez.
    // Doğrulanmayanlar: ENDOWMENT ve VSAM'a yazılmaması, LGSTSQ hata mesajları (API'den görülmez).
    [Fact]
    [Trait("Operasyon", "hayat-ekle")]
    public async Task LGAPDB01_satir_296_hayat_musteri_yok_70_doner()
    {
        using var cevap = await _istemci.PostAsJsonAsync(
            HayatYardimcilari.HayatAdresi(PoliceYardimcilari.AtanmayanNumara), HayatVerisi.Police4);

        await cevap.HataGovdesiDogrulaAsync(HttpStatusCode.NotFound, "customer-not-found", "70");
    }

    // Tahmin 12-21, lgapdb01.cbl:311 (CA-POLICY-NUM, IDENTITY_VAL_LOCAL()'in verdiği yeni numarayla ezilir, :308-311;
    // INSERT numarayı DEFAULT ile alır, :279). Girdideki numara (tahminde var olan poliçe 4) kullanılmaz.
    // API karşılığı: policyNumber istek gövdesinde gönderilirse yok sayılır, istek reddedilmez (api-design.md §1).
    // Kurulum: müşteri ve poliçe POST ile kurulur; ikinci POST'un gövdesinde ilk poliçenin numarası gönderilir.
    // Beklenen: 201 (§2.4, §3); cevaptaki policyNumber gönderilen numaradan farklı.
    // Doğrulanmayanlar: tahminin CA-LASTCHANGED kısmı (girdideki zaman damgası da yok sayılır, :284, :316-321): POST'ta
    // lastChanged gönderilirse ne olacağı api-design.md'de tanımlı değil (§1 yalnızca customerNumber ve policyNumber'ı
    // sayar), bu yüzden gönderilmez. Var olan poliçenin değişmediği (GET gerektirir; hayat-ekle testi hayat-sorgula'ya
    // bağlanmaz). Yeni numaranın değeri ([B6]).
    [Fact]
    [Trait("Operasyon", "hayat-ekle")]
    public async Task LGAPDB01_satir_311_hayat_girdideki_numara_yok_sayilir_00_doner()
    {
        var musteriNumarasi = await _istemci.MusteriKurAsync(MusteriVerisi.AndrewPandy);
        var varOlanPolice = await _istemci.HayatPoliceKurAsync(musteriNumarasi, HayatVerisi.Police4);
        var govde = new Dictionary<string, object>(HayatVerisi.Police4) { ["policyNumber"] = varOlanPolice };

        using var cevap = await _istemci.PostAsJsonAsync(HayatYardimcilari.HayatAdresi(musteriNumarasi), govde);

        await cevap.DurumKoduDogrulaAsync(HttpStatusCode.Created);
        Assert.NotEqual(varOlanPolice, PoliceYardimcilari.PoliceNumarasiOku(await cevap.JsonOkuAsync()));
    }

    // Tahmin 12-27, lgapdb01.cbl:364 (ENDOWMENT INSERT'e CA-E-LIFE-ASSURED'ın 31 baytı gider; alan X(31),
    // lgcmarea.cpy:53, sütun CHAR(31), db2cre.jcl:211). Girdi: poliçe 4, ama yaşam sigortalısı 31 karakter
    // (HayatVerisi.OtuzBirKarakterliYasamSigortalisi).
    // Eski sistemde kesilme yalnızca VSAM tarafındaydı: KSDSPOLY kaydında yaşam sigortalısı 30 bayttır (lgapvs01.cbl:40,
    // :115) ve 31. karakter (5) kayda yazılmaz. Yeni sistemde VSAM yoktur. Bu test yalnızca POST cevabındaki echo'yu ölçer:
    // cevapta lifeAssured 31 karakterin hepsiyle döner. Değerin veritabanında 31 karakter saklandığını görmek GET gerektirir;
    // hayat-ekle testi hayat-sorgula'ya bağlanmasın diye GET yapılmaz.
    // Beklenen: 201 (api-design.md §2.4, §3); cevapta lifeAssured 31 karakter (§4.5: en fazla 31).
    // Doğrulanmayanlar: veritabanında saklanan değer (GET yok), VSAM kaydı ve 30 baytlık kesilme (yeni sistemde VSAM yok),
    // diğer alanlar (12-01 ölçer), numaranın değeri ([B6]), lastChanged.
    [Fact]
    [Trait("Operasyon", "hayat-ekle")]
    public async Task LGAPDB01_satir_364_hayat_yasam_sigortalisi_31_karakter_00_doner()
    {
        var musteriNumarasi = await _istemci.MusteriKurAsync(MusteriVerisi.AndrewPandy);

        using var cevap = await _istemci.PostAsJsonAsync(
            HayatYardimcilari.HayatAdresi(musteriNumarasi), HayatVerisi.Police4OtuzBirKarakterliYasamSigortalisiyla);

        await cevap.DurumKoduDogrulaAsync(HttpStatusCode.Created);
        PoliceYardimcilari.PoliceAlanlariDogrula(
            await cevap.JsonOkuAsync(),
            new Dictionary<string, object> { ["lifeAssured"] = HayatVerisi.OtuzBirKarakterliYasamSigortalisi });
    }
}
