using System.Net;
using System.Net.Http.Json;
using GenApp.EquivalenceTests.Altyapi;

namespace GenApp.EquivalenceTests.Konut;

/// <summary>
/// Konut poliçesi güncelle: <c>PUT /api/customers/{customerNumber}/house-policies/{policyNumber}</c> (eski program
/// LGUPOL01 → LGUPDB01 → LGUPVS01). Tahminler: tests/predictions/10-house-update.md. Beklenen cevaplar:
/// docs/api-design.md §2.3, §3, §4.2, §4.4, §5.
/// Her test kendi müşterisini POST ile kurar (musteri-ekle). Kurulum için poliçe POST ile kurulur, güncelleme gövdesindeki
/// lastChanged GET ile alınır (§5); bu testler konut-ekle ve konut-sorgula etkin değilken kırmızı olur. Etkinleştirme
/// sırası: konut-ekle, konut-sorgula, konut-guncelle, konut-sil.
/// Ödeme (payment) güncellemede POST'taki değerle (0) gönderilir ve doğrulanmaz: eski sistem ödemeyi güncellemez
/// (lgupdb01.cbl:320-324, SET listesinde PAYMENT yok), ama bunun API'deki karşılığı belgede tanımsız olduğu için 10-18
/// atlandı.
/// </summary>
public sealed class KonutGuncelleTests : IClassFixture<GenAppApiFactory>
{
    private readonly HttpClient _istemci;

    public KonutGuncelleTests(GenAppApiFactory fabrika) => _istemci = fabrika.CreateClient();

    // Tahmin 10-01, lgupdb01.cbl:278 (CA-LASTCHANGED = DB2-LASTCHANGED: konut UPDATE'i (:291-293), POLICY UPDATE'i ve yeni
    // zaman damgası (:318-334); CA-RETURN-CODE '00' kalır, CLOSE :371'de yine '00' yazar). Tahmin poliçe 6'yı poliçe 8 ve
    // HOUSE 8'in değerleriyle günceller; poliçe 6'nın değerleriyle POST edilen poliçe, KonutVerisi.Police8 ile güncellenir.
    // Sıra 09-01 → 10-01 testin içinde kurulur: lastChanged GET ile alınır. Tahmindeki B4 (FETCH'in 5 sütun / 6 host
    // değişkeni uyumsuzluğu, 10 Kod gözlemleri 5) yeni sistemde yoktur.
    // Beklenen: 200 + poliçe (api-design.md §2.3, §3, §5); policyNumber ve customerNumber adresteki numaralar; PUT
    // cevabında ve ardından GET'te Police8'in alanları: tarihler yyyy-MM-dd (§1), metinler kırpılmış (ADR 0002 Karar 6).
    // Doğrulanmayanlar: VSAM KSDSPOLY kaydının yeniden yazılması (yeni sistemde VSAM yok), yeni lastChanged (değer ve
    // biçim), houseName ve brokersReference (boş gönderilir, boş metin beklentisi yazılmaz; KonutVerisi), payment (sınıf
    // açıklaması).
    [Fact]
    [Trait("Operasyon", "konut-guncelle")]
    public async Task LGUPDB01_satir_278_zaman_damgasi_esit_00_doner()
    {
        var musteriNumarasi = await _istemci.MusteriKurAsync(MusteriVerisi.AndrewPandy);
        var policeNumarasi = await _istemci.KonutPoliceKurAsync(musteriNumarasi, KonutVerisi.Police6);
        var lastChanged = PoliceYardimcilari.LastChangedOku(
            await _istemci.KonutPoliceOkuAsync(musteriNumarasi, policeNumarasi, "Ön koşul (lastChanged okuma)"));
        var beklenen = PoliceYardimcilari.AlanlarHaric(
            KonutVerisi.Police8, [.. KonutVerisi.BosGonderilenAlanlar, "payment"]);

        using var cevap = await _istemci.PutAsJsonAsync(
            KonutYardimcilari.KonutAdresiNumarayla(musteriNumarasi, policeNumarasi),
            PoliceYardimcilari.GuncellemeGovdesi(KonutVerisi.Police8, lastChanged));

        await cevap.DurumKoduDogrulaAsync(HttpStatusCode.OK);
        var police = await cevap.JsonOkuAsync();
        Assert.Equal(policeNumarasi, PoliceYardimcilari.PoliceNumarasiOku(police));
        Assert.Equal(musteriNumarasi, ApiYardimcilari.MusteriNumarasiOku(police));
        PoliceYardimcilari.PoliceAlanlariDogrula(police, beklenen);

        var okunan = await _istemci.KonutPoliceOkuAsync(musteriNumarasi, policeNumarasi, "Güncelleme sonrası okuma");
        PoliceYardimcilari.PoliceAlanlariDogrula(okunan, beklenen);
    }

    // Tahmin 10-04, lgupdb01.cbl:346 (zaman damgası eşleşmez: :278 → :344 ELSE → :346 '02'). Eski sistemde 02 çağırana
    // ulaşmaz: CLOSE-PCURSOR (:360) onu '00' ile ezer (:371) ve LGUPVS01 (:209) VSAM kaydını reddedilen isteğin verileriyle
    // yeniden yazar; Db2 değişmez. Eski sistem 00 döner ve VSAM'a yazar. Yeni sistem 409, concurrency-conflict, 02 döner:
    // api-design.md §3 "Bilinçli sapmalar" (:127, "Poliçe güncelle: lastChanged uyuşmuyor"), §5, ADR 0002 Karar 9. Bu test
    // yalnızca API eşlemesini ölçer; eski davranış (00 + VSAM yazılır) bilinçli sapmadır.
    // Sıra 09-01 → 10-01 → 10-04 testin içinde kurulur: eski lastChanged ilk GET'ten alınır; ilk PUT (10-01'in isteği,
    // KonutVerisi.Police8) onu eskitir. İkinci PUT aynı eski değerle 10-01'in isteğini HOUSE 6'nın konut alanlarıyla
    // gönderir (KonutVerisi.Police8OrtakAlanlariHouse6).
    // Beklenen: 409, type "concurrency-conflict", returnCode "02" (§3, :95). Ardından GET'te ilk PUT'un değerleri
    // (Police8) kalır; reddedilen isteğin konut alanları yazılmaz. Veritabanının değişmemesi eski sistemde de aynıdır.
    // Doğrulanmayanlar: VSAM (yeni sistemde yok), lastChanged, houseName ve brokersReference (boş), payment (sınıf
    // açıklaması).
    [Fact]
    [Trait("Operasyon", "konut-guncelle")]
    public async Task LGUPDB01_satir_346_zaman_damgasi_eski_02_doner()
    {
        var musteriNumarasi = await _istemci.MusteriKurAsync(MusteriVerisi.AndrewPandy);
        var policeNumarasi = await _istemci.KonutPoliceKurAsync(musteriNumarasi, KonutVerisi.Police6);
        var adres = KonutYardimcilari.KonutAdresiNumarayla(musteriNumarasi, policeNumarasi);
        var eskiLastChanged = PoliceYardimcilari.LastChangedOku(
            await _istemci.KonutPoliceOkuAsync(musteriNumarasi, policeNumarasi, "Ön koşul (lastChanged okuma)"));
        using (var ilkGuncelleme = await _istemci.PutAsJsonAsync(
                   adres, PoliceYardimcilari.GuncellemeGovdesi(KonutVerisi.Police8, eskiLastChanged)))
        {
            await ilkGuncelleme.DurumKoduDogrulaAsync(HttpStatusCode.OK, "Ön koşul (ilk güncelleme, 10-01)");
        }

        using var cevap = await _istemci.PutAsJsonAsync(
            adres, PoliceYardimcilari.GuncellemeGovdesi(KonutVerisi.Police8OrtakAlanlariHouse6, eskiLastChanged));

        await cevap.HataGovdesiDogrulaAsync(HttpStatusCode.Conflict, "concurrency-conflict", "02");

        var okunan = await _istemci.KonutPoliceOkuAsync(
            musteriNumarasi, policeNumarasi, "Reddedilen güncelleme sonrası okuma");
        PoliceYardimcilari.PoliceAlanlariDogrula(
            okunan,
            PoliceYardimcilari.AlanlarHaric(KonutVerisi.Police8, [.. KonutVerisi.BosGonderilenAlanlar, "payment"]));
    }

    // Tahmin 10-05, lgupdb01.cbl:352 (FETCH satır bulamaz, [Db2 bilgisi] SQLCODE = 100 (:351) → '01'). Eski sistemde 01
    // çağırana ulaşmaz: CLOSE (:360) onu '00' ile ezer (:371), ardından LGUPVS01 (:209) aynı anahtarla READ yapar, kayıt
    // yoktur: '81' ve ABEND LGV3 (lgupvs01.cbl:147-151). Çağıranın 81'i görmesi yalnızca düzenekte B8 ile mümkündür;
    // gerçek CICS'te ABEND görevi sonlandırır. api-design.md §3 (:128, "Poliçe güncelle: poliçe yok") eski sistemin 01'i
    // 00 ile ezip 00 döndürdüğünü söyler; bu ifade koda uymaz: ezilen 00'dan sonra VSAM okuması başarısız olur ve 81 +
    // ABEND gelir (10 Kod gözlemleri 18). Yeni sistem 404, not-found, 01 döner (§3, bilinçli sapma; ADR 0002 Karar 9).
    // Bu test yalnızca API eşlemesini ölçer.
    // Kurulum: müşteri ve poliçe POST ile kurulur; güncelleme gövdesi için geçerli bir lastChanged GET ile alınır
    // (değerin biçimini sunucu belirler, §5; uydurulmuş bir değer başka bir doğrulamaya takılabilir).
    // Numara seçimi: Tahmin poliçe 0 kullanır. PoliceYardimcilari.AtanmayanNumara (999999999): API'nin 0'ı doğrulamayla
    // 400 ile reddedip reddetmeyeceği tanımlı değil; dokuz hanelik numara DB2-POLICYNUM-INT'e (PIC S9(9) COMP,
    // lgupdb01.cbl:88) kesilmeden sığar; kimlik değeri 1000001'den başlar ve policyNumber istekte gönderilse de yok
    // sayılır (§1), bu numaraya testlerde pratikte ulaşılmaz.
    // Doğrulanmayanlar: kurulan poliçenin değişmediği (GET yok), VSAM, LGSTSQ hata mesajları.
    [Fact]
    [Trait("Operasyon", "konut-guncelle")]
    public async Task LGUPDB01_satir_352_police_yok_01_doner()
    {
        var musteriNumarasi = await _istemci.MusteriKurAsync(MusteriVerisi.AndrewPandy);
        var policeNumarasi = await _istemci.KonutPoliceKurAsync(musteriNumarasi, KonutVerisi.Police6);
        var lastChanged = PoliceYardimcilari.LastChangedOku(
            await _istemci.KonutPoliceOkuAsync(musteriNumarasi, policeNumarasi, "Ön koşul (lastChanged okuma)"));

        using var cevap = await _istemci.PutAsJsonAsync(
            KonutYardimcilari.KonutAdresiNumarayla(musteriNumarasi, PoliceYardimcilari.AtanmayanNumara),
            PoliceYardimcilari.GuncellemeGovdesi(KonutVerisi.Police8, lastChanged));

        await cevap.HataGovdesiDogrulaAsync(HttpStatusCode.NotFound, "not-found", "01");
    }

    // Tahmin 10-06, lgupdb01.cbl:352 (cursor müşteri ve poliçe numarasını birlikte ister, :135-137; [Db2 bilgisi] FETCH
    // satır bulamaz, SQLCODE = 100 → '01'). Eski sistemde 10-05 ile aynı yol: CLOSE 01'i 00 ile ezer, LGUPVS01 81 yazar ve
    // ABEND LGV3 ile durur (yalnızca B8 ile görünür); belgedeki "00 döner" ifadesi burada da koda uymaz. Yeni sistem:
    // 404, not-found, 01 (§3 "Poliçe güncelle: poliçe yok": B'nin adresinde bu poliçe yoktur). Yalnızca API eşlemesi
    // test edilir.
    // Kurulum: A müşterisi ve poliçesi (tahminde müşteri 4 ve poliçe 6), A'nın poliçesinin lastChanged'i ve B müşterisi
    // (tahminde müşteri 9) kurulur; A'nın poliçesi B'nin adresinden güncellenir.
    // Beklenen: 404, type "not-found", returnCode "01" (§3, :94).
    // Doğrulanmayanlar: A'nın poliçesinin değişmediği (GET yok), VSAM, LGSTSQ hata mesajları.
    [Fact]
    [Trait("Operasyon", "konut-guncelle")]
    public async Task LGUPDB01_satir_352_musteri_police_eslesmiyor_01_doner()
    {
        var musteriA = await _istemci.MusteriKurAsync(MusteriVerisi.AndrewPandy);
        var policeNumarasi = await _istemci.KonutPoliceKurAsync(musteriA, KonutVerisi.Police6);
        var lastChanged = PoliceYardimcilari.LastChangedOku(
            await _istemci.KonutPoliceOkuAsync(musteriA, policeNumarasi, "Ön koşul (lastChanged okuma)"));
        var musteriB = await _istemci.MusteriKurAsync(MusteriVerisi.AndrewPandy);

        using var cevap = await _istemci.PutAsJsonAsync(
            KonutYardimcilari.KonutAdresiNumarayla(musteriB, policeNumarasi),
            PoliceYardimcilari.GuncellemeGovdesi(KonutVerisi.Police8, lastChanged));

        await cevap.HataGovdesiDogrulaAsync(HttpStatusCode.NotFound, "not-found", "01");
    }
}
