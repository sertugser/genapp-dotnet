using System.Net;
using GenApp.IntegrationTests.Altyapi;

namespace GenApp.IntegrationTests.Motor;

/// <summary>
/// Motor poliçesi sorgula (FR-05; AD:44, <c>GET /api/customers/{c}/motor-policies/{p}</c> → 200 + poliçe) baştan sona.
/// Bağımlılık: ön koşullar <c>musteri-ekle</c> ve <c>motor-ekle</c> (POST) ile kurulur.
/// </summary>
[Collection(Koleksiyon.Motor)]
public sealed class MotorSorgulaTests(EntegrasyonFixture fixture)
{
    /// <summary>
    /// MO2. Kurulum: müşteri (N) ve motor poliçesi (P).
    /// Cevap: 200 (AD:44); alanlar eklenen değerler (AD §4.2-4.3), <c>customerNumber</c> = N, <c>policyNumber</c> = P;
    /// <c>lastChanged</c> ekleme cevabındakiyle aynı (ekleme yeni değeri döndürür, sorgulama kayıttakini: ADR 0003
    /// :141-142, AD:256). İki API değeri karşılaştırılır; veritabanındaki biçime bakılmaz.
    /// Veritabanı: MO1'de doğrulandı; burada okuma yolunun aynı kaydı döndürdüğü ölçülür.
    /// </summary>
    [Fact]
    [Trait("Operasyon", "motor-sorgula")]
    public async Task Motor_sorgula_eklenen_policeyi_ve_ayni_lastChanged_degerini_doner()
    {
        var n = await fixture.Istemci.MusteriKurAsync();
        var eklenen = TestVerisi.Motor();
        var (p, eklemeCevabi) = await fixture.Istemci.PoliceKurAsync(PoliceTuru.Motor, n, eklenen);
        var eklemedekiLastChanged = ApiYardimcilari.LastChangedOku(eklemeCevabi);

        using var cevap = await fixture.Istemci.GetAsync(ApiYardimcilari.PoliceAdresi(PoliceTuru.Motor, n, p));

        await cevap.DurumKoduDogrulaAsync(HttpStatusCode.OK);
        var govde = await cevap.JsonOkuAsync();
        ApiYardimcilari.AlanlariDogrula(govde, PoliceBeklentileri.Cevap(eklenen, n, p), "Poliçe alanları farklı");
        Assert.Equal(eklemedekiLastChanged, ApiYardimcilari.LastChangedOku(govde));
    }
}
