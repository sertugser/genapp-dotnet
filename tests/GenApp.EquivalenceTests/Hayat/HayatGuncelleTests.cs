using System.Net;
using System.Net.Http.Json;
using GenApp.EquivalenceTests.Altyapi;

namespace GenApp.EquivalenceTests.Hayat;

/// <summary>
/// Hayat sigortası poliçesi güncelle: <c>PUT /api/customers/{customerNumber}/endowment-policies/{policyNumber}</c> (eski
/// program LGUPOL01 → LGUPDB01 → LGUPVS01). Tahminler: tests/predictions/14-endowment-update.md. Beklenen cevaplar:
/// docs/api-design.md §2.4, §3, §4.2, §4.5, §5.
/// Her test kendi müşterisini POST ile kurar (musteri-ekle). Kurulum için poliçe POST ile kurulur, güncelleme gövdesindeki
/// lastChanged GET ile alınır (§5); bu testler hayat-ekle ve hayat-sorgula etkin değilken kırmızı olur. Etkinleştirme sırası:
/// hayat-ekle, hayat-sorgula, hayat-guncelle, hayat-sil.
/// </summary>
public sealed class HayatGuncelleTests : IClassFixture<GenAppApiFactory>
{
    private readonly HttpClient _istemci;

    public HayatGuncelleTests(GenAppApiFactory fabrika) => _istemci = fabrika.CreateClient();

    // Tahmin 14-01, lgupdb01.cbl:278 (CA-LASTCHANGED = DB2-LASTCHANGED: ENDOWMENT UPDATE'i (:394-406), POLICY UPDATE'i ve
    // yeni zaman damgası (:318-334); CA-RETURN-CODE '00' kalır, CLOSE :371'de yine '00' yazar). Tahmin poliçe 4'ü poliçe 5
    // ve ENDOWMENT 5'in değerleriyle günceller; poliçe 4'ün değerleriyle POST edilen poliçe, HayatVerisi.Police5 ile
    // güncellenir.
    // Sıra 13-01 → 14-01 testin içinde kurulur: lastChanged GET ile alınır.
    // Beklenen: 200 + poliçe (api-design.md §2.4, §3, §5); policyNumber ve customerNumber adresteki numaralar; PUT cevabında
    // ve ardından GET'te Police5'in alanları: tarihler yyyy-MM-dd (§1), metinler kırpılmış (ADR 0002 Karar 6). GET'teki
    // lastChanged eskisinden farklı (tahmin: "güncelleme anındaki zaman damgası, eskisinden farklı").
    // Doğrulanmayanlar: VSAM KSDSPOLY kaydının yeniden yazılması (yeni sistemde VSAM yok), yeni lastChanged'in değeri ve
    // biçimi, brokersReference (boş gönderilir, boş metin beklentisi yazılmaz; HayatVerisi).
    [Fact]
    [Trait("Operasyon", "hayat-guncelle")]
    public async Task LGUPDB01_satir_278_hayat_zaman_damgasi_esit_00_doner()
    {
        var musteriNumarasi = await _istemci.MusteriKurAsync(MusteriVerisi.AndrewPandy);
        var policeNumarasi = await _istemci.HayatPoliceKurAsync(musteriNumarasi, HayatVerisi.Police4);
        var eskiLastChanged = PoliceYardimcilari.LastChangedOku(
            await _istemci.HayatPoliceOkuAsync(musteriNumarasi, policeNumarasi, "Ön koşul (lastChanged okuma)"));
        var beklenen = PoliceYardimcilari.AlanlarHaric(HayatVerisi.Police5, HayatVerisi.BosGonderilenAlanlar);

        using var cevap = await _istemci.PutAsJsonAsync(
            HayatYardimcilari.HayatAdresiNumarayla(musteriNumarasi, policeNumarasi),
            PoliceYardimcilari.GuncellemeGovdesi(HayatVerisi.Police5, eskiLastChanged));

        await cevap.DurumKoduDogrulaAsync(HttpStatusCode.OK);
        var police = await cevap.JsonOkuAsync();
        Assert.Equal(policeNumarasi, PoliceYardimcilari.PoliceNumarasiOku(police));
        Assert.Equal(musteriNumarasi, ApiYardimcilari.MusteriNumarasiOku(police));
        PoliceYardimcilari.PoliceAlanlariDogrula(police, beklenen);

        var okunan = await _istemci.HayatPoliceOkuAsync(musteriNumarasi, policeNumarasi, "Güncelleme sonrası okuma");
        PoliceYardimcilari.PoliceAlanlariDogrula(okunan, beklenen);
        Assert.NotEqual(eskiLastChanged, PoliceYardimcilari.LastChangedOku(okunan));
    }

    // Tahmin 14-02, lgupdb01.cbl:346 (zaman damgası eşleşmez: :278 → :344 ELSE → :346 '02'). Eski sistemde 02 çağırana
    // ulaşmaz: CLOSE-PCURSOR (:360) onu '00' ile ezer (:371) ve LGUPVS01 (:209) VSAM kaydını reddedilen isteğin verileriyle
    // yeniden yazar; Db2 değişmez. Eski sistem 00 döner ve VSAM'a yazar. Yeni sistem 409, concurrency-conflict, 02 döner:
    // api-design.md §3 "Bilinçli sapmalar" ("Poliçe güncelle: lastChanged uyuşmuyor"), §5, ADR 0002 Karar 9. Bu test
    // yalnızca API eşlemesini ölçer; eski davranış (00 + VSAM yazılır) bilinçli sapmadır.
    // Sıra 13-01 → 14-01 → 14-02 testin içinde kurulur: eski lastChanged ilk GET'ten alınır; ilk PUT (14-01'in isteği,
    // HayatVerisi.Police5) onu eskitir. İkinci PUT aynı eski değerle 14-01'in isteğini ENDOWMENT 4'ün hayat sigortası
    // alanlarıyla gönderir (HayatVerisi.Police5OrtakAlanlariHayat4).
    // Beklenen: 409, type "concurrency-conflict", returnCode "02" (§3). Ardından GET'te ilk PUT'un değerleri (Police5)
    // kalır; reddedilen isteğin hayat sigortası alanları yazılmaz. Veritabanının değişmemesi eski sistemde de aynıdır.
    // Doğrulanmayanlar: VSAM (yeni sistemde yok), lastChanged, brokersReference (boş).
    [Fact]
    [Trait("Operasyon", "hayat-guncelle")]
    public async Task LGUPDB01_satir_346_hayat_zaman_damgasi_eski_02_doner()
    {
        var musteriNumarasi = await _istemci.MusteriKurAsync(MusteriVerisi.AndrewPandy);
        var policeNumarasi = await _istemci.HayatPoliceKurAsync(musteriNumarasi, HayatVerisi.Police4);
        var adres = HayatYardimcilari.HayatAdresiNumarayla(musteriNumarasi, policeNumarasi);
        var eskiLastChanged = PoliceYardimcilari.LastChangedOku(
            await _istemci.HayatPoliceOkuAsync(musteriNumarasi, policeNumarasi, "Ön koşul (lastChanged okuma)"));
        using (var ilkGuncelleme = await _istemci.PutAsJsonAsync(
                   adres, PoliceYardimcilari.GuncellemeGovdesi(HayatVerisi.Police5, eskiLastChanged)))
        {
            await ilkGuncelleme.DurumKoduDogrulaAsync(HttpStatusCode.OK, "Ön koşul (ilk güncelleme, 14-01)");
        }

        using var cevap = await _istemci.PutAsJsonAsync(
            adres, PoliceYardimcilari.GuncellemeGovdesi(HayatVerisi.Police5OrtakAlanlariHayat4, eskiLastChanged));

        await cevap.HataGovdesiDogrulaAsync(HttpStatusCode.Conflict, "concurrency-conflict", "02");

        var okunan = await _istemci.HayatPoliceOkuAsync(
            musteriNumarasi, policeNumarasi, "Reddedilen güncelleme sonrası okuma");
        PoliceYardimcilari.PoliceAlanlariDogrula(
            okunan,
            PoliceYardimcilari.AlanlarHaric(HayatVerisi.Police5, HayatVerisi.BosGonderilenAlanlar));
    }

    // Tahmin 14-03, lgupdb01.cbl:352 (FETCH satır bulamaz, [Db2 bilgisi] SQLCODE = 100 (:351) → '01'). Eski sistemde 01
    // çağırana ulaşmaz: CLOSE (:360) onu '00' ile ezer (:371), ardından LGUPVS01 (:209) aynı anahtarla READ yapar, kayıt
    // yoktur: '81' ve ABEND LGV3 (lgupvs01.cbl:147-151). Çağıranın 81'i görmesi yalnızca düzenekte mümkündür; gerçek CICS'te
    // ABEND görevi sonlandırır. Yeni sistem 404, not-found, 01 döner (api-design.md §3, "Poliçe güncelle: poliçe yok",
    // bilinçli sapma; ADR 0002 Karar 9). Bu test yalnızca API eşlemesini ölçer.
    // Kurulum: müşteri ve poliçe POST ile kurulur; güncelleme gövdesi için geçerli bir lastChanged GET ile alınır (değerin
    // biçimini sunucu belirler, §5; uydurulmuş bir değer başka bir doğrulamaya takılabilir).
    // Numara seçimi: Tahmin poliçe 999 kullanır (var olmayan bir numara). PoliceYardimcilari.AtanmayanNumara
    // (999999999): dokuz hanelik numara DB2-POLICYNUM-INT'e (PIC S9(9) COMP, lgupdb01.cbl:88) kesilmeden sığar; kimlik
    // değeri 1000001'den başlar ve policyNumber istekte gönderilse de yok sayılır (§1), bu numaraya testlerde pratikte
    // ulaşılmaz.
    // Doğrulanmayanlar: kurulan poliçenin değişmediği (GET yok), VSAM, LGSTSQ hata mesajları.
    [Fact]
    [Trait("Operasyon", "hayat-guncelle")]
    public async Task LGUPDB01_satir_352_hayat_police_yok_01_doner()
    {
        var musteriNumarasi = await _istemci.MusteriKurAsync(MusteriVerisi.AndrewPandy);
        var policeNumarasi = await _istemci.HayatPoliceKurAsync(musteriNumarasi, HayatVerisi.Police4);
        var lastChanged = PoliceYardimcilari.LastChangedOku(
            await _istemci.HayatPoliceOkuAsync(musteriNumarasi, policeNumarasi, "Ön koşul (lastChanged okuma)"));

        using var cevap = await _istemci.PutAsJsonAsync(
            HayatYardimcilari.HayatAdresiNumarayla(musteriNumarasi, PoliceYardimcilari.AtanmayanNumara),
            PoliceYardimcilari.GuncellemeGovdesi(HayatVerisi.Police5, lastChanged));

        await cevap.HataGovdesiDogrulaAsync(HttpStatusCode.NotFound, "not-found", "01");
    }

    // Tahmin 14-04, lgupdb01.cbl:352 (cursor müşteri ve poliçe numarasını birlikte ister, :128-137; [Db2 bilgisi] FETCH
    // satır bulamaz, SQLCODE = 100 → '01'). Eski sistemde 14-03 ile aynı yol: CLOSE 01'i 00 ile ezer, LGUPVS01 81 yazar ve
    // ABEND LGV3 ile durur. Yeni sistem: 404, not-found, 01 (api-design.md §3 "Poliçe güncelle: poliçe yok": B'nin
    // adresinde bu poliçe yoktur). Yalnızca API eşlemesi test edilir.
    // Kurulum: A müşterisi ve poliçesi (tahminde müşteri 8 ve poliçe 4), A'nın poliçesinin lastChanged'i ve B müşterisi
    // (tahminde müşteri 3) kurulur; A'nın poliçesi B'nin adresinden güncellenir.
    // Beklenen: 404, type "not-found", returnCode "01" (§3).
    // Doğrulanmayanlar: A'nın poliçesinin değişmediği (GET yok), VSAM, LGSTSQ hata mesajları.
    [Fact]
    [Trait("Operasyon", "hayat-guncelle")]
    public async Task LGUPDB01_satir_352_hayat_musteri_police_eslesmiyor_01_doner()
    {
        var musteriA = await _istemci.MusteriKurAsync(MusteriVerisi.AndrewPandy);
        var policeNumarasi = await _istemci.HayatPoliceKurAsync(musteriA, HayatVerisi.Police4);
        var lastChanged = PoliceYardimcilari.LastChangedOku(
            await _istemci.HayatPoliceOkuAsync(musteriA, policeNumarasi, "Ön koşul (lastChanged okuma)"));
        var musteriB = await _istemci.MusteriKurAsync(MusteriVerisi.AndrewPandy);

        using var cevap = await _istemci.PutAsJsonAsync(
            HayatYardimcilari.HayatAdresiNumarayla(musteriB, policeNumarasi),
            PoliceYardimcilari.GuncellemeGovdesi(HayatVerisi.Police5, lastChanged));

        await cevap.HataGovdesiDogrulaAsync(HttpStatusCode.NotFound, "not-found", "01");
    }

    // Tahmin 14-27, lgupdb01.cbl:320 (POLICY UPDATE'inin SET listesi :320-324'te ISSUEDATE, EXPIRYDATE, LASTCHANGED,
    // BROKERID ve BROKERSREFERENCE'ı yazar; PAYMENT yok, :314'teki taşıma kullanılmaz). Girdideki ödeme (123) yok sayılır;
    // Db2'deki 0 kalır. api-design.md §3 "Poliçe güncelle: payment" korunan davranıştır: istekteki payment yok sayılır,
    // cevapta kayıttaki değer döner.
    // Kurulum: poliçe 4'ün değerleriyle (payment 0) POST edilen poliçe, HayatVerisi.Police5OdemeliYuzYirmiUc ile güncellenir
    // (payment 123); lastChanged GET ile alınır.
    // Beklenen: 200 (api-design.md §2.4, §3); PUT cevabında ve ardından GET'te payment 0; diğer alanlar 14-01 gibi
    // güncellenir.
    // Doğrulanmayanlar: VSAM, lastChanged, brokersReference (boş).
    [Fact]
    [Trait("Operasyon", "hayat-guncelle")]
    public async Task LGUPDB01_satir_320_hayat_odeme_guncellenmez_00_doner()
    {
        var musteriNumarasi = await _istemci.MusteriKurAsync(MusteriVerisi.AndrewPandy);
        var policeNumarasi = await _istemci.HayatPoliceKurAsync(musteriNumarasi, HayatVerisi.Police4);
        var lastChanged = PoliceYardimcilari.LastChangedOku(
            await _istemci.HayatPoliceOkuAsync(musteriNumarasi, policeNumarasi, "Ön koşul (lastChanged okuma)"));
        var beklenen = PoliceYardimcilari.AlanlarHaric(HayatVerisi.Police5, HayatVerisi.BosGonderilenAlanlar);

        using var cevap = await _istemci.PutAsJsonAsync(
            HayatYardimcilari.HayatAdresiNumarayla(musteriNumarasi, policeNumarasi),
            PoliceYardimcilari.GuncellemeGovdesi(HayatVerisi.Police5OdemeliYuzYirmiUc, lastChanged));

        await cevap.DurumKoduDogrulaAsync(HttpStatusCode.OK);
        PoliceYardimcilari.PoliceAlanlariDogrula(await cevap.JsonOkuAsync(), beklenen);

        var okunan = await _istemci.HayatPoliceOkuAsync(musteriNumarasi, policeNumarasi, "Güncelleme sonrası okuma");
        PoliceYardimcilari.PoliceAlanlariDogrula(okunan, beklenen);
    }

    // Tahmin 14-29, lgupdb01.cbl:403 (UPDATE ENDOWMENT'ın SET listesi LIFEASSURED = :CA-E-LIFE-ASSURED'ı X(31) olarak
    // yazar; sütun CHAR(31), db2cre.jcl:211). Girdi: 14-01'in isteği, ama yaşam sigortalısı 31 karakter
    // (HayatVerisi.Police5OtuzBirKarakterliYasamSigortalisiyla).
    // Eski sistemde kesilme yalnızca VSAM tarafındaydı: KSDSPOLY kaydında yaşam sigortalısı 30 bayttır (lgupvs01.cbl:41,
    // :118) ve 31. karakter (5) kayda yazılmaz. Yeni sistemde VSAM yoktur; test yalnızca Db2 tarafını ölçer.
    // Beklenen: 200 (api-design.md §2.4, §3); PUT cevabında ve ardından GET'te lifeAssured 31 karakterin hepsiyle.
    // Doğrulanmayanlar: VSAM kaydı ve 30 baytlık kesilme (yeni sistemde VSAM yok), diğer alanlar (14-01 ölçer), lastChanged.
    [Fact]
    [Trait("Operasyon", "hayat-guncelle")]
    public async Task LGUPDB01_satir_403_hayat_yasam_sigortalisi_31_karakter_00_doner()
    {
        var musteriNumarasi = await _istemci.MusteriKurAsync(MusteriVerisi.AndrewPandy);
        var policeNumarasi = await _istemci.HayatPoliceKurAsync(musteriNumarasi, HayatVerisi.Police4);
        var lastChanged = PoliceYardimcilari.LastChangedOku(
            await _istemci.HayatPoliceOkuAsync(musteriNumarasi, policeNumarasi, "Ön koşul (lastChanged okuma)"));
        var beklenen = new Dictionary<string, object>
        {
            ["lifeAssured"] = HayatVerisi.OtuzBirKarakterliYasamSigortalisi,
        };

        using var cevap = await _istemci.PutAsJsonAsync(
            HayatYardimcilari.HayatAdresiNumarayla(musteriNumarasi, policeNumarasi),
            PoliceYardimcilari.GuncellemeGovdesi(
                HayatVerisi.Police5OtuzBirKarakterliYasamSigortalisiyla, lastChanged));

        await cevap.DurumKoduDogrulaAsync(HttpStatusCode.OK);
        PoliceYardimcilari.PoliceAlanlariDogrula(await cevap.JsonOkuAsync(), beklenen);

        var okunan = await _istemci.HayatPoliceOkuAsync(musteriNumarasi, policeNumarasi, "Güncelleme sonrası okuma");
        PoliceYardimcilari.PoliceAlanlariDogrula(okunan, beklenen);
    }
}
