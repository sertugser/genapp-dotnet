using System.Net;
using System.Net.Http.Json;
using GenApp.EquivalenceTests.Altyapi;

namespace GenApp.EquivalenceTests.Musteri;

/// <summary>
/// Müşteri güncelle: <c>PUT /api/customers/{customerNumber}</c> (eski program LGUCUS01 → LGUCDB01 → LGUCVS01).
/// Tahminler: tests/predictions/03-customer-update.md. Beklenen cevaplar: docs/api-design.md §2.1, §3, §4.1.
/// Kurulum için POST, kalıcılık kontrolü için GET gerekir; bu test musteri-ekle ve musteri-sorgula etkin
/// değilken kırmızı olur. Etkinleştirme sırası: musteri-ekle, musteri-sorgula, musteri-guncelle.
/// </summary>
public sealed class MusteriGuncelleTests : IClassFixture<GenAppApiFactory>
{
    private readonly HttpClient _istemci;

    public MusteriGuncelleTests(GenAppApiFactory fabrika) => _istemci = fabrika.CreateClient();

    // Tahmin 03-01, lgucdb01.cbl:124 (CA-RETURN-CODE '00'; UPDATE SQLCODE = 0 ise değişmez, :171). UPDATE dokuz
    // sütunun hepsini yazar (:155-169). Müşteri 1'e güvenilmez: veritabanında örnek verinin yüklü olduğu
    // belgelerde kesin değil (SRS §2.1 ve db2cre.jcl farklı kaynaklar). Bu yüzden onun değerleriyle POST edilen
    // müşteri, müşteri 2'nin değerleriyle güncellenir (MusteriVerisi.ScottTracey). api-design.md boş metin davranışını
    // tanımlamıyor; 03-01 testi bu belirsizliğe bağlanmasın diye phoneMobile boş olmayan değerle gönderilir. Boş
    // değerin ezilmesi (03 Kod gözlemleri 7) bu testte sınanmamaktadır.
    // Beklenen: 200 + müşteri (api-design.md §2.1, §3), metinler kırpılmış (ADR 0002 Karar 6). Güncellemenin
    // kalıcı olduğu ayrıca GET ile doğrulanır.
    // Doğrulanmayanlar: VSAM KSDSCUST kaydı (yeni sistemde VSAM yok), CA-NUM-POLICIES (API'de karşılığı yok);
    // ikisi de API'den gözlemlenemez.
    [Fact]
    [Trait("Operasyon", "musteri-guncelle")]
    public async Task LGUCDB01_satir_124_musteri_var_00_doner()
    {
        var musteriNumarasi = await _istemci.MusteriKurAsync(MusteriVerisi.AndrewPandy);
        var adres = ApiYardimcilari.MusteriAdresiNumarayla(musteriNumarasi);

        using var cevap = await _istemci.PutAsJsonAsync(adres, MusteriVerisi.ScottTracey);

        await cevap.DurumKoduDogrulaAsync(HttpStatusCode.OK);
        var musteri = await cevap.JsonOkuAsync();
        Assert.Equal(musteriNumarasi, ApiYardimcilari.MusteriNumarasiOku(musteri));
        ApiYardimcilari.MusteriAlanlariDogrula(musteri, MusteriVerisi.ScottTracey);

        using var okuma = await _istemci.GetAsync(adres);

        await okuma.DurumKoduDogrulaAsync(HttpStatusCode.OK, "Güncelleme sonrası okuma");
        ApiYardimcilari.MusteriAlanlariDogrula(await okuma.JsonOkuAsync(), MusteriVerisi.ScottTracey);
    }
}
