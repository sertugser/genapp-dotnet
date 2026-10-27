using System.Net;
using GenApp.IntegrationTests.Altyapi;

namespace GenApp.IntegrationTests.Konut;

/// <summary>
/// Konut poliçesi sil (FR-11; AD:55, <c>DELETE /api/customers/{c}/house-policies/{p}</c> → 204) baştan sona.
/// Bağımlılık: ön koşullar <c>musteri-ekle</c> ve <c>konut-ekle</c> (POST) ile kurulur; K6 ayrıca <c>motor-ekle</c>
/// (motor poliçesi motor adresinden POST ile kurulur) operasyonuna bağlıdır.
/// </summary>
[Collection(Koleksiyon.Konut)]
public sealed class KonutSilTests(EntegrasyonFixture fixture)
{
    /// <summary>
    /// K4. Kurulum: müşteri (N) ve konut poliçesi (P). İstek: P'yi sil.
    /// Cevap: 204 (AD:55).
    /// Veritabanı: <c>policy</c> ve <c>house</c> tablolarında P için 0 satır (tür tablosundaki satır
    /// <c>ON DELETE CASCADE</c> ile gider, DS:56-57); <c>customer</c> N için tam 1 satır (müşteri silme kapsam dışı, SRS 6).
    /// </summary>
    [Fact]
    [Trait("Operasyon", "konut-sil")]
    public async Task Konut_sil_policy_ve_house_satirlarini_siler_musteri_kalir()
    {
        var n = await fixture.Istemci.MusteriKurAsync();
        var (p, _) = await fixture.Istemci.PoliceKurAsync(PoliceTuru.Konut, n, TestVerisi.Konut());

        using var cevap = await fixture.Istemci.DeleteAsync(ApiYardimcilari.PoliceAdresi(PoliceTuru.Konut, n, p));

        await cevap.DurumKoduDogrulaAsync(HttpStatusCode.NoContent);
        Assert.Equal(0, await fixture.Veritabani.SayAsync(Sema.Policy.Tablo, (Sema.Policy.PolicyNumber, p)));
        Assert.Equal(0, await fixture.Veritabani.SayAsync(Sema.House.Tablo, (Sema.House.PolicyNumber, p)));
        await fixture.Veritabani.TekSatirOkuAsync(Sema.Customer.Tablo, Sema.Customer.CustomerNumber, n);
    }

    /// <summary>
    /// K6 (dilimler arası). Kurulum: müşteri (N) ve motor adresinden eklenen motor poliçesi (P; <c>policy_type</c> 'M').
    /// İstek: P'yi KONUT adresinden sil.
    /// Cevap: 404, <c>type: not-found</c>, <c>returnCode: "01"</c>: <c>policyType</c> adresteki türle (konut <c>H</c>)
    /// uyuşmuyorsa işlem yapılmaz (AD:136; ADR 0002 Karar 9 :91-93; eşleme AD:94).
    /// Veritabanı: P'nin <c>policy</c> ve <c>motor</c> satırları duruyor (tam 1'er satır): yanlış türden poliçe silinmez.
    /// </summary>
    [Fact]
    [Trait("Operasyon", "konut-sil")]
    public async Task Konut_adresinden_motor_policesi_silinmez_404_doner()
    {
        var n = await fixture.Istemci.MusteriKurAsync();
        var (p, _) = await fixture.Istemci.PoliceKurAsync(PoliceTuru.Motor, n, TestVerisi.Motor());

        using var cevap = await fixture.Istemci.DeleteAsync(ApiYardimcilari.PoliceAdresi(PoliceTuru.Konut, n, p));

        await cevap.HataGovdesiDogrulaAsync(HttpStatusCode.NotFound, "not-found", "01");
        await fixture.Veritabani.TekSatirOkuAsync(Sema.Policy.Tablo, Sema.Policy.PolicyNumber, p);
        await fixture.Veritabani.TekSatirOkuAsync(Sema.Motor.Tablo, Sema.Motor.PolicyNumber, p);
    }
}
