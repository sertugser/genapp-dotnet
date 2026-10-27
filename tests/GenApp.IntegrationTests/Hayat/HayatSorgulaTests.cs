using System.Net;
using GenApp.IntegrationTests.Altyapi;

namespace GenApp.IntegrationTests.Hayat;

/// <summary>
/// Hayat sigortası poliçesi sorgula (FR-13; AD:62, <c>GET /api/customers/{c}/endowment-policies/{p}</c> → 200 + poliçe)
/// baştan sona. Bağımlılık: ön koşullar <c>musteri-ekle</c> ve <c>hayat-ekle</c> (POST) ile kurulur.
/// </summary>
[Collection(Koleksiyon.Hayat)]
public sealed class HayatSorgulaTests(EntegrasyonFixture fixture)
{
    /// <summary>
    /// H2. Kurulum: müşteri (N) ve hayat poliçesi (P).
    /// Cevap: 200 (AD:62); alanlar eklenen değerler (AD §4.2, §4.5), <c>customerNumber</c> = N, <c>policyNumber</c> = P;
    /// <c>lastChanged</c> ekleme cevabındakiyle aynı (ADR 0003 :141-142, AD:256). İki API değeri karşılaştırılır.
    /// Veritabanı: H1'de doğrulandı; burada okuma yolunun aynı kaydı döndürdüğü ölçülür.
    /// </summary>
    [Fact]
    [Trait("Operasyon", "hayat-sorgula")]
    public async Task Hayat_sorgula_eklenen_policeyi_ve_ayni_lastChanged_degerini_doner()
    {
        var n = await fixture.Istemci.MusteriKurAsync();
        var eklenen = TestVerisi.Hayat();
        var (p, eklemeCevabi) = await fixture.Istemci.PoliceKurAsync(PoliceTuru.Hayat, n, eklenen);
        var eklemedekiLastChanged = ApiYardimcilari.LastChangedOku(eklemeCevabi);

        using var cevap = await fixture.Istemci.GetAsync(ApiYardimcilari.PoliceAdresi(PoliceTuru.Hayat, n, p));

        await cevap.DurumKoduDogrulaAsync(HttpStatusCode.OK);
        var govde = await cevap.JsonOkuAsync();
        ApiYardimcilari.AlanlariDogrula(govde, PoliceBeklentileri.Cevap(eklenen, n, p), "Poliçe alanları farklı");
        Assert.Equal(eklemedekiLastChanged, ApiYardimcilari.LastChangedOku(govde));
    }
}
