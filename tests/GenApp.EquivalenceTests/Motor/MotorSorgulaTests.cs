using System.Net;
using GenApp.EquivalenceTests.Altyapi;

namespace GenApp.EquivalenceTests.Motor;

/// <summary>
/// Motor poliçesi sorgula: <c>GET /api/customers/{customerNumber}/motor-policies/{policyNumber}</c>
/// (eski program LGIPOL01 → LGIPDB01, istek kodu <c>01IMOT</c>).
/// Tahminler: tests/predictions/05-motor-inquire.md. Beklenen cevaplar: docs/api-design.md §2.2, §3, §4.2, §4.3.
/// Kurulum için müşteri ve poliçe <c>POST</c> ile kurulur; bu testler musteri-ekle ve motor-ekle etkin değilken
/// kırmızı olur. Etkinleştirme sırası: musteri-ekle, motor-ekle, motor-sorgula, motor-guncelle, motor-sil.
/// Hangi tahmin satırlarının çevrilmediği ve nedeni: Motor/README.md.
/// </summary>
public sealed class MotorSorgulaTests : IClassFixture<GenAppApiFactory>
{
    private readonly HttpClient _istemci;

    public MotorSorgulaTests(GenAppApiFactory fabrika) => _istemci = fabrika.CreateClient();

    // Tahmin 05-01, lgipdb01.cbl:258 (CA-RETURN-CODE '00'; SELECT SQLCODE = 0 ise değişmez, :572). Tahminde örnek
    // verideki müşteri 2'nin poliçe 1'i sorgulanır. Örnek verinin veritabanında yüklü olduğu belgelerde kesin değil
    // (SRS §2.1 ve db2cre.jcl farklı kaynaklar); bu yüzden ona güvenilmez: poliçe 1 ve motor 1'in değerleriyle
    // (db2cre.jcl:945-954, :1105-1114; MotorVerisi.Ford) yeni bir müşteriye POST ile kurulan poliçe sorgulanır.
    // Beklenen: 200 + poliçe (api-design.md §2.2, §3), metinler kırpılmış (ADR 0002 Karar 6), tarihler yyyy-MM-dd (§1):
    // ortak ve motor alanları kurulanla aynı, customerNumber ve policyNumber kurulanlar, lastChanged boş olmayan metin.
    // Doğrulanmayanlar: CA-M-FILLER'daki "FINAL" işareti (API'de karşılığı yok), tarih ve zaman damgalarının Db2
    // biçimi (tahminde belirsiz; API tarihleri yyyy-MM-dd, lastChanged'ı sunucunun biçimiyle verir, §5), COMMAREA'daki
    // bayt düzeni.
    [Fact]
    [Trait("Operasyon", "motor-sorgula")]
    public async Task LGIPDB01_satir_258_police_var_00_doner()
    {
        var musteriNumarasi = await _istemci.MusteriKurAsync(MusteriVerisi.AndrewPandy);
        var policeNumarasi = await _istemci.MotorPoliceKurAsync(musteriNumarasi, MotorVerisi.Ford);

        using var cevap = await _istemci.GetAsync(MotorYardimcilari.MotorAdresiNumarayla(musteriNumarasi, policeNumarasi));

        await cevap.DurumKoduDogrulaAsync(HttpStatusCode.OK);
        var police = await cevap.JsonOkuAsync();
        Assert.Equal(policeNumarasi, MotorYardimcilari.PoliceNumarasiOku(police));
        Assert.Equal(musteriNumarasi, ApiYardimcilari.MusteriNumarasiOku(police));
        MotorYardimcilari.MotorAlanlariDogrula(police, MotorVerisi.Ford);
        MotorYardimcilari.LastChangedOku(police);
    }

    // Tahmin 05-02, lgipdb01.cbl:598-599 (CA-M-PREMIUM ve CA-M-ACCIDENTS, DB2-M-PREMIUM-INT ve DB2-M-ACCIDENTS-INT'ten
    // ayrıca doldurulur; çünkü CA-MOTOR'a yalnızca ilk 65 bayt kopyalanır, :602). Bu satır, prim ve kaza sayısının
    // sıfırdan farklı değerlerle doğru eşlendiğini sınar: poliçe 3 ve motor 3 (db2cre.jcl:969-978, :1128-1137;
    // MotorVerisi.Dennis: prim 2850, kaza 12). Ön koşul 05-01 ile aynı biçimde POST ile kurulur.
    // Beklenen: 200 + poliçe, bütün alanlar kurulanla aynı (api-design.md §2.2, §3, §4.3).
    // Doğrulanmayanlar: 05-01'dekiler.
    [Fact]
    [Trait("Operasyon", "motor-sorgula")]
    public async Task LGIPDB01_satir_598_prim_ve_kaza_sifirdan_farkli_00_doner()
    {
        var musteriNumarasi = await _istemci.MusteriKurAsync(MusteriVerisi.AndrewPandy);
        var policeNumarasi = await _istemci.MotorPoliceKurAsync(musteriNumarasi, MotorVerisi.Dennis);

        using var cevap = await _istemci.GetAsync(MotorYardimcilari.MotorAdresiNumarayla(musteriNumarasi, policeNumarasi));

        await cevap.DurumKoduDogrulaAsync(HttpStatusCode.OK);
        var police = await cevap.JsonOkuAsync();
        MotorYardimcilari.MotorAlanlariDogrula(police, MotorVerisi.Dennis);
    }

    // Tahmin 05-03, lgipdb01.cbl:612 (SELECT SQLCODE = 100 → CA-RETURN-CODE '01'; bu dalda hata mesajı yazılmaz ve
    // alanlara dokunulmaz, :608-612). Müşteri vardır, poliçe yoktur.
    // Beklenen: 404, type "not-found", returnCode "01" (api-design.md §3). Gövdedeki status alanı yalnızca §3'teki
    // örnekte geçtiği için doğrulanmaz.
    // Numara seçimi: tahmin poliçe 999'u kullanır (JCL'de yok). Burada 999999999 kullanılır: geçerli ama hiç atanmayan
    // bir numara (kimlik değeri 1000001'den başlar, docs/database-schema.md; istekte numara alanı yoktur, §4.2);
    // API'nin küçük numarayı doğrulamayla reddedip reddetmeyeceği tanımlı olmadığından test "kayıt yok" kuralını ölçer.
    // Doğrulanmayanlar: hata mesajının yazılmaması (kuyruk, API'den gözlemlenmez).
    [Fact]
    [Trait("Operasyon", "motor-sorgula")]
    public async Task LGIPDB01_satir_612_police_yok_01_doner()
    {
        var musteriNumarasi = await _istemci.MusteriKurAsync(MusteriVerisi.AndrewPandy);

        using var cevap = await _istemci.GetAsync(MotorYardimcilari.MotorAdresiNumarayla(musteriNumarasi, 999999999));

        await cevap.HataGovdesiDogrulaAsync(HttpStatusCode.NotFound, "not-found", "01");
    }

    // Tahmin 05-04, lgipdb01.cbl:612 (SELECT SQLCODE = 100 → '01'). SELECT'in WHERE koşulu müşteri numarasını da
    // içerir (:564-569): poliçe başka bir müşteriye aitse satır bulunmaz. Tahminde poliçe 1 müşteri 2'ye aittir ve
    // müşteri 3 ile sorgulanır (db2cre.jcl:946); burada iki müşteri POST ile kurulur, poliçe birincisine eklenir,
    // ikincisinin adresinden sorgulanır.
    // Beklenen: 404, type "not-found", returnCode "01" (api-design.md §3).
    // Doğrulanmayanlar: yok (cevap, tahminin bütün gözlemlenebilir kısmını kapsar).
    [Fact]
    [Trait("Operasyon", "motor-sorgula")]
    public async Task LGIPDB01_satir_612_baska_musterinin_policesi_01_doner()
    {
        var sahibi = await _istemci.MusteriKurAsync(MusteriVerisi.AndrewPandy);
        var baskasi = await _istemci.MusteriKurAsync(MusteriVerisi.ScottTracey);
        var policeNumarasi = await _istemci.MotorPoliceKurAsync(sahibi, MotorVerisi.Ford);

        using var cevap = await _istemci.GetAsync(MotorYardimcilari.MotorAdresiNumarayla(baskasi, policeNumarasi));

        await cevap.HataGovdesiDogrulaAsync(HttpStatusCode.NotFound, "not-found", "01");
    }
}
