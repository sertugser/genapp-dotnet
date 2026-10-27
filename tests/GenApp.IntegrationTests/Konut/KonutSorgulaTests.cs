using System.Net;
using GenApp.IntegrationTests.Altyapi;

namespace GenApp.IntegrationTests.Konut;

/// <summary>
/// Konut poliçesi sorgula (FR-09; AD:53, <c>GET /api/customers/{c}/house-policies/{p}</c> → 200 + poliçe) baştan sona.
/// Bağımlılık: ön koşullar <c>musteri-ekle</c> ve <c>konut-ekle</c> (POST) ile kurulur.
/// </summary>
[Collection(Koleksiyon.Konut)]
public sealed class KonutSorgulaTests(EntegrasyonFixture fixture)
{
    /// <summary>
    /// K2. Kurulum: müşteri (N) ve konut poliçesi (P).
    /// Cevap: 200 (AD:53); alanlar eklenen değerler (AD §4.2, §4.4), <c>customerNumber</c> = N, <c>policyNumber</c> = P;
    /// <c>lastChanged</c> ekleme cevabındakiyle aynı (ADR 0003 :141-142, AD:256). İki API değeri karşılaştırılır.
    /// Veritabanı: K1'de doğrulandı; burada okuma yolunun aynı kaydı döndürdüğü ölçülür.
    /// </summary>
    [Fact]
    [Trait("Operasyon", "konut-sorgula")]
    public async Task Konut_sorgula_eklenen_policeyi_ve_ayni_lastChanged_degerini_doner()
    {
        var n = await fixture.Istemci.MusteriKurAsync();
        var eklenen = TestVerisi.Konut();
        var (p, eklemeCevabi) = await fixture.Istemci.PoliceKurAsync(PoliceTuru.Konut, n, eklenen);
        var eklemedekiLastChanged = ApiYardimcilari.LastChangedOku(eklemeCevabi);

        using var cevap = await fixture.Istemci.GetAsync(ApiYardimcilari.PoliceAdresi(PoliceTuru.Konut, n, p));

        await cevap.DurumKoduDogrulaAsync(HttpStatusCode.OK);
        var govde = await cevap.JsonOkuAsync();
        ApiYardimcilari.AlanlariDogrula(govde, PoliceBeklentileri.Cevap(eklenen, n, p), "Poliçe alanları farklı");
        Assert.Equal(eklemedekiLastChanged, ApiYardimcilari.LastChangedOku(govde));
    }
}
