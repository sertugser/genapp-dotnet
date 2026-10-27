using System.Net;
using GenApp.IntegrationTests.Altyapi;

namespace GenApp.IntegrationTests.Hayat;

/// <summary>
/// Hayat sigortası poliçesi sil (FR-15; AD:64, <c>DELETE /api/customers/{c}/endowment-policies/{p}</c> → 204) baştan
/// sona. Bağımlılık: ön koşullar <c>musteri-ekle</c> ve <c>hayat-ekle</c> (POST) ile kurulur.
/// </summary>
[Collection(Koleksiyon.Hayat)]
public sealed class HayatSilTests(EntegrasyonFixture fixture)
{
    /// <summary>
    /// H4. Kurulum: müşteri (N) ve hayat poliçesi (P). İstek: P'yi sil.
    /// Cevap: 204 (AD:64).
    /// Veritabanı: <c>policy</c> ve <c>endowment</c> tablolarında P için 0 satır (tür tablosundaki satır
    /// <c>ON DELETE CASCADE</c> ile gider, DS:56-57); <c>customer</c> N için tam 1 satır (müşteri silme kapsam dışı, SRS 6).
    /// </summary>
    [Fact]
    [Trait("Operasyon", "hayat-sil")]
    public async Task Hayat_sil_policy_ve_endowment_satirlarini_siler_musteri_kalir()
    {
        var n = await fixture.Istemci.MusteriKurAsync();
        var (p, _) = await fixture.Istemci.PoliceKurAsync(PoliceTuru.Hayat, n, TestVerisi.Hayat());

        using var cevap = await fixture.Istemci.DeleteAsync(ApiYardimcilari.PoliceAdresi(PoliceTuru.Hayat, n, p));

        await cevap.DurumKoduDogrulaAsync(HttpStatusCode.NoContent);
        Assert.Equal(0, await fixture.Veritabani.SayAsync(Sema.Policy.Tablo, (Sema.Policy.PolicyNumber, p)));
        Assert.Equal(0, await fixture.Veritabani.SayAsync(Sema.Endowment.Tablo, (Sema.Endowment.PolicyNumber, p)));
        await fixture.Veritabani.TekSatirOkuAsync(Sema.Customer.Tablo, Sema.Customer.CustomerNumber, n);
    }
}
