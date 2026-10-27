using System.Net;
using GenApp.IntegrationTests.Altyapi;

namespace GenApp.IntegrationTests.Musteri;

/// <summary>
/// Müşteri sorgula (FR-02; AD:36, <c>GET /api/customers/{customerNumber}</c> → 200 + müşteri) baştan sona.
/// Bağımlılık: ön koşul müşterisi <c>musteri-ekle</c> (POST) ile kurulur.
/// </summary>
[Collection(Koleksiyon.Musteri)]
public sealed class MusteriSorgulaTests(EntegrasyonFixture fixture)
{
    /// <summary>
    /// M2. Kurulum: müşteri POST ile eklenir (N).
    /// Cevap: 200 (AD:36); <c>customerNumber</c> = N ve alanlar eklenen değerler (AD §4.1, metinler kırpılmış AD:18).
    /// Veritabanı: <c>customer</c> tablosunda N için tam 1 satır ve değerleri aynı beklentiyle (cevap ile satır aynı
    /// kaydı gösterir; okuma yolu baştan sona).
    /// </summary>
    [Fact]
    [Trait("Operasyon", "musteri-sorgula")]
    public async Task Musteri_sorgula_veritabanindaki_satiri_doner()
    {
        var eklenen = TestVerisi.Musteri();
        var n = await fixture.Istemci.MusteriKurAsync(eklenen);

        using var cevap = await fixture.Istemci.GetAsync(ApiYardimcilari.MusteriAdresiNumarayla(n));

        await cevap.DurumKoduDogrulaAsync(HttpStatusCode.OK);
        var govde = await cevap.JsonOkuAsync();
        var beklenen = MusteriBeklentileri.Cevap(eklenen);
        beklenen["customerNumber"] = n;
        ApiYardimcilari.AlanlariDogrula(govde, beklenen, "Müşteri alanları farklı");

        var satir = await fixture.Veritabani.TekSatirOkuAsync(Sema.Customer.Tablo, Sema.Customer.CustomerNumber, n);
        VeritabaniYardimcilari.SatirDogrula(satir, MusteriBeklentileri.Satir(eklenen), Sema.Customer.Tablo);
    }
}
