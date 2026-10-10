using System.Net;
using System.Net.Http.Json;
using GenApp.EquivalenceTests.Altyapi;

namespace GenApp.EquivalenceTests.Musteri;

/// <summary>
/// Müşteri ekle: <c>POST /api/customers</c> (eski program LGACUS01 → LGACDB01).
/// Tahminler: tests/predictions/01-customer-add.md. Beklenen cevaplar: docs/api-design.md §2.1, §3, §4.1.
/// </summary>
public sealed class MusteriEkleTests : IClassFixture<GenAppApiFactory>
{
    private readonly HttpClient _istemci;

    public MusteriEkleTests(GenAppApiFactory fabrika) => _istemci = fabrika.CreateClient();

    // Tahmin 01-01, lgacdb01.cbl:156 (CA-RETURN-CODE '00'; INSERT başarılıysa değişmez, :245). Yeni numara
    // CA-CUSTOMER-NUM'a :285'te yazılır. Kodda kişi tekrarı kontrolü yok: aynı kişi zaten varken (01-01'de
    // müşteri 1) ikinci ekleme yeni bir numarayla açılır. Ön koşul olan kişi POST ile kurulur.
    // Beklenen: 201 + müşteri (api-design.md §2.1, §3), metinler kırpılmış (ADR 0002 Karar 6).
    // Doğrulanmayanlar: numaranın değeri (tahminde sayaçtan 11 [B7]; yeni sistemde numara veritabanından gelir),
    // CA-NUM-POLICIES, KSDSCUST ve CUSTOMER_SECURE kayıtları (API'den gözlemlenmez), Location başlığı.
    [Fact]
    [Trait("Operasyon", "musteri-ekle")]
    public async Task LGACDB01_satir_156_musteri_eklenir_00_doner()
    {
        var ilkNumara = await _istemci.MusteriKurAsync(MusteriVerisi.AndrewPandy);

        using var cevap = await _istemci.PostAsJsonAsync(ApiYardimcilari.MusteriAdresi, MusteriVerisi.AndrewPandy);

        await cevap.DurumKoduDogrulaAsync(HttpStatusCode.Created);
        var musteri = await cevap.JsonOkuAsync();
        Assert.NotEqual(ilkNumara, ApiYardimcilari.MusteriNumarasiOku(musteri));
        ApiYardimcilari.MusteriAlanlariDogrula(musteri, MusteriVerisi.AndrewPandy);
    }
}
