using System.Net;
using GenApp.EquivalenceTests.Altyapi;

namespace GenApp.EquivalenceTests.Musteri;

/// <summary>
/// Müşteri sorgula: <c>GET /api/customers/{customerNumber}</c> (eski program LGICUS01 → LGICDB01).
/// Tahminler: tests/predictions/02-customer-inquire.md. Beklenen cevaplar: docs/api-design.md §2.1, §3, §4.1.
/// Kurulum için POST gerekir (02-01); bu test musteri-ekle etkin değilken kırmızı olur. Etkinleştirme sırası:
/// musteri-ekle, musteri-sorgula, musteri-guncelle.
/// </summary>
public sealed class MusteriSorgulaTests : IClassFixture<GenAppApiFactory>
{
    private readonly HttpClient _istemci;

    public MusteriSorgulaTests(GenAppApiFactory fabrika) => _istemci = fabrika.CreateClient();

    // Tahmin 02-01, lgicdb01.cbl:194 (SELECT SQLCODE = 0 → CA-RETURN-CODE '00').
    // Müşteri 1'e güvenilmez: veritabanında örnek verinin yüklü olduğu belgelerde kesin değil (SRS §2.1 ve
    // db2cre.jcl farklı kaynaklar). Bu yüzden müşteri 1'in değerleriyle (db2cre.jcl:437-446) POST edilen yeni
    // müşteri sorgulanır.
    // Beklenen: 200 + müşteri (api-design.md §2.1, §3), metinler kırpılmış (ADR 0002 Karar 6), tarih yyyy-MM-dd (§1).
    // CA-NUM-POLICIES'in API'de karşılığı olmadığı için doğrulanmaz.
    [Fact]
    [Trait("Operasyon", "musteri-sorgula")]
    public async Task LGICDB01_satir_194_musteri_var_00_doner()
    {
        var musteriNumarasi = await _istemci.MusteriKurAsync(MusteriVerisi.AndrewPandy);

        using var cevap = await _istemci.GetAsync(ApiYardimcilari.MusteriAdresiNumarayla(musteriNumarasi));

        await cevap.DurumKoduDogrulaAsync(HttpStatusCode.OK);
        var musteri = await cevap.JsonOkuAsync();
        Assert.Equal(musteriNumarasi, ApiYardimcilari.MusteriNumarasiOku(musteri));
        ApiYardimcilari.MusteriAlanlariDogrula(musteri, MusteriVerisi.AndrewPandy);
    }

    // Tahmin 02-02, lgicdb01.cbl:196 (SELECT SQLCODE = 100 → CA-RETURN-CODE '01').
    // Beklenen: 404, type "not-found", returnCode "01" (api-design.md §3). Gövdedeki status alanı yalnızca §3'teki
    // örnekte geçtiği için doğrulanmaz.
    // Numara seçimi: Tahmin 0 kullanır. API'nin 0 gibi pozitif olmayan bir numarayı doğrulamayla 400 ile
    // reddedip reddetmeyeceği api-design.md'de tanımlı değil; bu test o kuralı değil "kayıt yok" kuralını
    // ölçmeli. Bu yüzden geçerli ama hiç atanmayan bir numara seçildi:
    // - 999999999 dokuz hanedir, DB2-CUSTOMERNUMBER-INT'e (PIC S9(9) COMP, lgicdb01.cbl:72) kesilmeden sığar.
    //   COBOL'da da aynı yol çalışır: satır yok, SQLCODE = 100.
    // - Kimlik değeri 1000001'den başlar (docs/database-schema.md) ve istekte müşteri numarası alanı yoktur
    //   (§4.1); bu numaraya testlerde pratikte ulaşılmaz.
    [Fact]
    [Trait("Operasyon", "musteri-sorgula")]
    public async Task LGICDB01_satir_196_kayit_yok_01_doner()
    {
        using var cevap = await _istemci.GetAsync(ApiYardimcilari.MusteriAdresiNumarayla(999999999));

        await cevap.HataGovdesiDogrulaAsync(HttpStatusCode.NotFound, "not-found", "01");
    }
}
