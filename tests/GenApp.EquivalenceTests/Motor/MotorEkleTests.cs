using System.Net;
using System.Net.Http.Json;
using GenApp.EquivalenceTests.Altyapi;

namespace GenApp.EquivalenceTests.Motor;

/// <summary>
/// Motor poliçesi ekle: <c>POST /api/customers/{customerNumber}/motor-policies</c>
/// (eski program LGAPOL01 → LGAPDB01 → LGAPVS01, istek kodu <c>01AMOT</c>).
/// Tahminler: tests/predictions/04-motor-add.md. Beklenen cevaplar: docs/api-design.md §2.2, §3, §4.2, §4.3.
/// Kurulum için <c>POST /api/customers</c> gerekir; bu testler musteri-ekle etkin değilken kırmızı olur.
/// Etkinleştirme sırası: musteri-ekle, motor-ekle, motor-sorgula, motor-guncelle, motor-sil.
/// Hangi tahmin satırlarının çevrilmediği ve nedeni: Motor/README.md.
/// </summary>
public sealed class MotorEkleTests : IClassFixture<GenAppApiFactory>
{
    private readonly HttpClient _istemci;

    public MotorEkleTests(GenAppApiFactory fabrika) => _istemci = fabrika.CreateClient();

    // Tahmin 04-01, lgapdb01.cbl:293 (INSERT-POLICY, SQLCODE = 0 → CA-RETURN-CODE '00'; sonraki MOTOR INSERT'i
    // :473 ve LGAPVS01 :142 başarılıysa değişmez). Ön koşul: müşteri POST ile kurulur (tahminde müşteri 2, örnek
    // veride). Girdi, poliçe 1 ve motor 1'in değerleridir (db2cre.jcl:945-954, :1105-1114; MotorVerisi.Ford).
    // Kodda girdideki CA-POLICY-NUM kullanılmaz; yeni numara kimlikten gelir (:279, :309-311).
    // Beklenen: 201 + poliçe (api-design.md §2.2, §3), metinler kırpılmış (ADR 0002 Karar 6): ortak ve motor
    // alanları gönderilenle aynı, customerNumber kurulan müşteri, policyNumber tam sayı, lastChanged boş olmayan metin.
    // Doğrulanmayanlar: poliçe numarasının değeri (tahminde [B6] 1000001; yeni sistemde numara veritabanından gelir,
    // yalnızca pozitif tam sayı olduğuna bakılır), VSAM KSDSPOLY kaydı (yeni sistemde VSAM yok), commission sütunu
    // (INSERT listesinde yok, API'de karşılığı yok), lastChanged'ın biçimi ve değeri (tahminde belirsiz: Db2'nin
    // TIMESTAMP metni; biçimini sunucu belirler, api-design.md §5), Location başlığı.
    [Fact]
    [Trait("Operasyon", "motor-ekle")]
    public async Task LGAPDB01_satir_293_police_eklenir_00_doner()
    {
        var musteriNumarasi = await _istemci.MusteriKurAsync(MusteriVerisi.AndrewPandy);

        using var cevap = await _istemci.PostAsJsonAsync(
            MotorYardimcilari.MotorAdresi(musteriNumarasi), MotorVerisi.Ford);

        await cevap.DurumKoduDogrulaAsync(HttpStatusCode.Created);
        var police = await cevap.JsonOkuAsync();
        Assert.True(MotorYardimcilari.PoliceNumarasiOku(police) > 0, $"policyNumber pozitif değil. Gövde: {police.GetRawText()}");
        Assert.Equal(musteriNumarasi, ApiYardimcilari.MusteriNumarasiOku(police));
        MotorYardimcilari.MotorAlanlariDogrula(police, MotorVerisi.Ford);
        MotorYardimcilari.LastChangedOku(police);
    }

    // Tahmin 04-11, lgapdb01.cbl:296 (INSERT-POLICY, SQLCODE = -530 → CA-RETURN-CODE '70'; hemen ardından hata
    // mesajı yazılıp RETURN yapılır, MOTOR INSERT'ine ve LGAPVS01'e geçilmez, :295-298). Tahminin girdisi: müşteri 0
    // (yok) ve [B4] düzeneğin -530 üretmesi. Bu testte B4'ün karşılığı "var olmayan müşteri"dir: -530'un yabancı
    // anahtar ihlali olduğu COBOL'da yazmaz (04-12 bu yüzden belirsiz), ama POLICY tablosunun müşteriye yabancı anahtarı
    // vardır (db2cre.jcl:174-176) ve api-design.md §3 "70"i "poliçe eklenirken müşteri bulunamadı" diye tanımlar.
    // Aynı girdiyi (olmayan müşteri) 04-12 de anlatır; ayrıca çevrilmez.
    // Beklenen: 404, type "customer-not-found", returnCode "70" (api-design.md §3). "not-found" ve "01" değil: aynı
    // HTTP koduna düşen iki durumu returnCode ayırır.
    // Numara seçimi: 999999999, tahminin 0'ı yerine kullanılır. API'nin 0 gibi pozitif olmayan bir numarayı
    // doğrulamayla 400 ile reddedip reddetmeyeceği tanımlı değil; bu test o kuralı değil "müşteri yok" kuralını
    // ölçmeli. 999999999 dokuz hanedir (DB2-CUSTOMERNUM-INT'e, PIC S9(9) COMP, kesilmeden sığar: lgapdb01.cbl:90),
    // kimlik değeri 1000001'den başlar (docs/database-schema.md) ve istekte müşteri numarası alanı yoktur (§4.1);
    // bu numaraya testlerde pratikte ulaşılmaz.
    // Doğrulanmayanlar: hata mesajının kuyruğa yazılması (LGSTSQ), MOTOR INSERT'inin ve VSAM yazmanın yapılmaması
    // (API'den gözlemlenmez).
    [Fact]
    [Trait("Operasyon", "motor-ekle")]
    public async Task LGAPDB01_satir_296_musteri_yok_70_doner()
    {
        using var cevap = await _istemci.PostAsJsonAsync(MotorYardimcilari.MotorAdresi(999999999), MotorVerisi.Ford);

        await cevap.HataGovdesiDogrulaAsync(HttpStatusCode.NotFound, "customer-not-found", "70");
    }
}
