using System.Net;
using GenApp.IntegrationTests.Altyapi;

namespace GenApp.IntegrationTests.Motor;

/// <summary>
/// Motor poliçesi sil (FR-07; AD:46, <c>DELETE /api/customers/{c}/motor-policies/{p}</c> → 204) baştan sona.
/// Bağımlılık: ön koşullar <c>musteri-ekle</c> ve <c>motor-ekle</c> (POST) ile kurulur.
/// </summary>
[Collection(Koleksiyon.Motor)]
public sealed class MotorSilTests(EntegrasyonFixture fixture)
{
    /// <summary>
    /// MO5. Kurulum: müşteri (N) ve aynı müşteriye iki motor poliçesi (P1, P2). İstek: P1'i sil.
    /// Cevap: 204 (AD:46).
    /// Veritabanı: <c>policy</c> ve <c>motor</c> tablolarında P1 için 0 satır (tür tablosundaki satır yabancı anahtarın
    /// <c>ON DELETE CASCADE</c> kuralıyla gider, DS:56-57). P2'nin <c>policy</c> ve <c>motor</c> satırları duruyor (tam 1'er
    /// satır); <c>customer</c> N için tam 1 satır (poliçe silmek müşteriyi silmez; müşteri silme kapsam dışı, SRS 6).
    /// </summary>
    [Fact]
    [Trait("Operasyon", "motor-sil")]
    public async Task Motor_sil_policy_ve_motor_satirlarini_siler_musteri_ve_diger_police_kalir()
    {
        var n = await fixture.Istemci.MusteriKurAsync();
        var (p1, _) = await fixture.Istemci.PoliceKurAsync(PoliceTuru.Motor, n, TestVerisi.Motor());
        var (p2, _) = await fixture.Istemci.PoliceKurAsync(PoliceTuru.Motor, n, TestVerisi.Motor());

        using var cevap = await fixture.Istemci.DeleteAsync(ApiYardimcilari.PoliceAdresi(PoliceTuru.Motor, n, p1));

        await cevap.DurumKoduDogrulaAsync(HttpStatusCode.NoContent);
        Assert.Equal(0, await fixture.Veritabani.SayAsync(Sema.Policy.Tablo, (Sema.Policy.PolicyNumber, p1)));
        Assert.Equal(0, await fixture.Veritabani.SayAsync(Sema.Motor.Tablo, (Sema.Motor.PolicyNumber, p1)));
        await fixture.Veritabani.TekSatirOkuAsync(Sema.Policy.Tablo, Sema.Policy.PolicyNumber, p2);
        await fixture.Veritabani.TekSatirOkuAsync(Sema.Motor.Tablo, Sema.Motor.PolicyNumber, p2);
        await fixture.Veritabani.TekSatirOkuAsync(Sema.Customer.Tablo, Sema.Customer.CustomerNumber, n);
    }
}
