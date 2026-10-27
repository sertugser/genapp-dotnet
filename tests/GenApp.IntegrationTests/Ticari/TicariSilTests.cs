using System.Net;
using GenApp.IntegrationTests.Altyapi;

namespace GenApp.IntegrationTests.Ticari;

/// <summary>
/// Ticari poliçe sil (FR-18; AD:75, <c>DELETE /api/customers/{c}/commercial-policies/{p}</c> → 204) baştan sona.
/// Bağımlılık: ön koşullar <c>musteri-ekle</c> ve <c>ticari-ekle</c> (POST) ile kurulur.
/// </summary>
[Collection(Koleksiyon.Ticari)]
public sealed class TicariSilTests(EntegrasyonFixture fixture)
{
    /// <summary>
    /// T8. Kurulum: müşteri (N) ve ticari poliçe (P). İstek: P'yi sil.
    /// Cevap: 204 (AD:75).
    /// Veritabanı: <c>policy</c> ve <c>commercial</c> tablolarında P için 0 satır (tür tablosundaki satır
    /// <c>ON DELETE CASCADE</c> ile gider, DS:56-57); <c>customer</c> N için tam 1 satır (müşteri silme kapsam dışı, SRS 6).
    /// </summary>
    [Fact]
    [Trait("Operasyon", "ticari-sil")]
    public async Task Ticari_sil_policy_ve_commercial_satirlarini_siler_musteri_kalir()
    {
        var n = await fixture.Istemci.MusteriKurAsync();
        var (p, _) = await fixture.Istemci.PoliceKurAsync(PoliceTuru.Ticari, n, TestVerisi.Ticari(TestVerisi.PostaKodu()));

        using var cevap = await fixture.Istemci.DeleteAsync(ApiYardimcilari.PoliceAdresi(PoliceTuru.Ticari, n, p));

        await cevap.DurumKoduDogrulaAsync(HttpStatusCode.NoContent);
        Assert.Equal(0, await fixture.Veritabani.SayAsync(Sema.Policy.Tablo, (Sema.Policy.PolicyNumber, p)));
        Assert.Equal(0, await fixture.Veritabani.SayAsync(Sema.Commercial.Tablo, (Sema.Commercial.PolicyNumber, p)));
        await fixture.Veritabani.TekSatirOkuAsync(Sema.Customer.Tablo, Sema.Customer.CustomerNumber, n);
    }
}
