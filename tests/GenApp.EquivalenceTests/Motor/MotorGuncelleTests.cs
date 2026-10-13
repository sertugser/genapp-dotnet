using System.Net;
using System.Net.Http.Json;
using GenApp.EquivalenceTests.Altyapi;

namespace GenApp.EquivalenceTests.Motor;

/// <summary>
/// Motor poliçesi güncelle: <c>PUT /api/customers/{customerNumber}/motor-policies/{policyNumber}</c>
/// (eski program LGUPOL01 → LGUPDB01 → LGUPVS01, istek kodu <c>01UMOT</c>).
/// Tahminler: tests/predictions/06-motor-update.md. Beklenen cevaplar: docs/api-design.md §2.2, §3, §4.2, §4.3, §5.
/// Kurulum için müşteri ve poliçe <c>POST</c> ile kurulur, <c>lastChanged</c> ve kalıcılık kontrolü için <c>GET</c>
/// gerekir; bu testler musteri-ekle, motor-ekle ve motor-sorgula etkin değilken kırmızı olur. Etkinleştirme sırası:
/// musteri-ekle, motor-ekle, motor-sorgula, motor-guncelle, motor-sil.
/// Hangi tahmin satırlarının çevrilmediği ve nedeni: Motor/README.md.
/// </summary>
public sealed class MotorGuncelleTests : IClassFixture<GenAppApiFactory>
{
    private readonly HttpClient _istemci;

    public MotorGuncelleTests(GenAppApiFactory fabrika) => _istemci = fabrika.CreateClient();

    // Tahmin 06-01, lgupdb01.cbl:371 (CLOSE-PCURSOR, CLOSE SQLCODE = 0 → CA-RETURN-CODE '00'; OPEN :261, MOTOR UPDATE
    // :469-483 ve POLICY UPDATE :318-326 başarılıysa değişmez). Zaman damgası eşleştiği için :278'deki kontrol geçer.
    // Ön koşul: müşteri ve poliçe 1'in değerleriyle (MotorVerisi.Ford) poliçe POST ile kurulur, GET ile okunur
    // (CA-LASTCHANGED'ın Db2'deki değer olması için; api-design.md §5: değer aynen geri gönderilir). Girdi: poliçe 3 ve
    // motor 3'ün değerleri (MotorVerisi.Dennis; db2cre.jcl:969-978, :1128-1137). Böylece her girdinin JCL'de bir
    // kaynağı var.
    // payment, kurulandakiyle aynı (0) gönderilir: eski program PUT'ta payment'ı Db2'ye yazmaz (06-27; lgupdb01.cbl:314,
    // :320-324). Yeni sistemin bunu taklit edip etmeyeceği ayrı bir karardır (Motor/README.md); bu test ona bağlanmaz.
    // Beklenen: 200 + poliçe (api-design.md §2.2, §3), metinler kırpılmış (ADR 0002 Karar 6): bütün alanlar gönderilenle
    // aynı, customerNumber ve policyNumber değişmedi, lastChanged eskisinden farklı (tahmin: "güncelleme anındaki zaman
    // damgası, eskisinden farklı"). Güncellemenin kalıcı olduğu ayrıca GET ile doğrulanır.
    // Doğrulanmayanlar: VSAM KSDSPOLY kaydının yeniden yazılması (yeni sistemde VSAM yok), yeni lastChanged'ın değeri ve
    // biçimi (yalnızca eskisinden farklı olduğuna bakılır; biçimi sunucu belirler, §5), CUSTOMER tablosunun
    // değişmemesi.
    [Fact]
    [Trait("Operasyon", "motor-guncelle")]
    public async Task LGUPDB01_satir_371_police_guncellenir_00_doner()
    {
        var musteriNumarasi = await _istemci.MusteriKurAsync(MusteriVerisi.AndrewPandy);
        var policeNumarasi = await _istemci.MotorPoliceKurAsync(musteriNumarasi, MotorVerisi.Ford);
        var adres = MotorYardimcilari.MotorAdresiNumarayla(musteriNumarasi, policeNumarasi);
        var onceki = await _istemci.MotorPoliceOkuAsync(musteriNumarasi, policeNumarasi, "Ön koşul (poliçe okuma)");
        var eskiLastChanged = MotorYardimcilari.LastChangedOku(onceki);

        using var cevap = await _istemci.PutAsJsonAsync(
            adres, MotorYardimcilari.GuncellemeGovdesi(MotorVerisi.Dennis, eskiLastChanged));

        await cevap.DurumKoduDogrulaAsync(HttpStatusCode.OK);
        var police = await cevap.JsonOkuAsync();
        Assert.Equal(policeNumarasi, MotorYardimcilari.PoliceNumarasiOku(police));
        Assert.Equal(musteriNumarasi, ApiYardimcilari.MusteriNumarasiOku(police));
        MotorYardimcilari.MotorAlanlariDogrula(police, MotorVerisi.Dennis);
        Assert.NotEqual(eskiLastChanged, MotorYardimcilari.LastChangedOku(police));

        var sonra = await _istemci.MotorPoliceOkuAsync(musteriNumarasi, policeNumarasi, "Güncelleme sonrası okuma");
        MotorYardimcilari.MotorAlanlariDogrula(sonra, MotorVerisi.Dennis);
    }

    // Tahmin 06-02, lgupdb01.cbl:346 (CA-LASTCHANGED ile DB2-LASTCHANGED eşit değilse CA-RETURN-CODE '02' atanır,
    // :278, :344-346; hiçbir UPDATE çalışmaz). Tahminin girdisi: kayıt, istemci okuduktan sonra başka bir istemci
    // tarafından güncellenmiş. Burada aynı senaryo iki sıralı istekle kurulur: poliçe okunur, ilk istemci eski zaman
    // damgasıyla güncelleme yapar (başarılı), ikinci istemci aynı eski zaman damgasıyla güncellemeye çalışır.
    //
    // ESKİ KODUN FİİLİ DAVRANIŞI FARKLIDIR (tahmin 06-02, "Kod gözlemleri" 4): '02' çağırana ulaşmaz. :360'taki
    // PERFORM CLOSE-PCURSOR, CLOSE başarılıysa :371'de CA-RETURN-CODE'u '00' yapar ve '02'yi ezer; çağıran '00' görür,
    // Db2 güncellenmez, VSAM kaydı yine yeniden yazılır. Bu test eski kodun o davranışını ölçmez: tahmini birebir
    // çevirmek "eski zaman damgasıyla gönderilen PUT 200 döner" beklentisini yazmak olurdu ve bu SRS (docs/srs.md:124-126:
    // "değiştirilmişse güncellemeyi yapmaz ve bunu bildirir"), api-design.md §5 ve ADR 0002 Karar 7 ile çelişir. Test,
    // bu belgelerin istediğini ölçer: güncelleme yapılmaz ve 409 (returnCode "02") döner. Bu BİLİNÇLİ BİR SAPMADIR;
    // eski sistemle bire bir eşdeğerlik değildir ve karar bekliyor (Motor/README.md, Açık sorular).
    //
    // Beklenen: 409, type "concurrency-conflict", returnCode "02" (api-design.md §3). İkinci güncelleme uygulanmaz:
    // poliçe, ilk güncellemenin değerlerinde kalır (Dennis; ikinci istek Beetle gönderir).
    // Doğrulanmayanlar: eski kodun VSAM'ı yine de yazması (yeni sistemde VSAM yok), eski kodun gerçekte döndürdüğü '00'
    // (yukarıda).
    [Fact]
    [Trait("Operasyon", "motor-guncelle")]
    public async Task LGUPDB01_satir_346_lastchanged_uyusmuyor_02_doner()
    {
        var musteriNumarasi = await _istemci.MusteriKurAsync(MusteriVerisi.AndrewPandy);
        var policeNumarasi = await _istemci.MotorPoliceKurAsync(musteriNumarasi, MotorVerisi.Ford);
        var adres = MotorYardimcilari.MotorAdresiNumarayla(musteriNumarasi, policeNumarasi);
        var onceki = await _istemci.MotorPoliceOkuAsync(musteriNumarasi, policeNumarasi, "Ön koşul (poliçe okuma)");
        var eskiLastChanged = MotorYardimcilari.LastChangedOku(onceki);

        using (var ilk = await _istemci.PutAsJsonAsync(
                   adres, MotorYardimcilari.GuncellemeGovdesi(MotorVerisi.Dennis, eskiLastChanged)))
        {
            await ilk.DurumKoduDogrulaAsync(HttpStatusCode.OK, "Ön koşul (ilk istemcinin güncellemesi)");
        }

        using var cevap = await _istemci.PutAsJsonAsync(
            adres, MotorYardimcilari.GuncellemeGovdesi(MotorVerisi.Beetle, eskiLastChanged));

        await cevap.HataGovdesiDogrulaAsync(HttpStatusCode.Conflict, "concurrency-conflict", "02");

        var sonra = await _istemci.MotorPoliceOkuAsync(musteriNumarasi, policeNumarasi, "Reddedilen güncelleme sonrası okuma");
        MotorYardimcilari.MotorAlanlariDogrula(sonra, MotorVerisi.Dennis);
    }
}
