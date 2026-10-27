using System.Net;
using System.Net.Http.Json;
using GenApp.IntegrationTests.Altyapi;

namespace GenApp.IntegrationTests.Motor;

/// <summary>
/// Motor poliçesi ekle (FR-04; AD:43, <c>POST /api/customers/{c}/motor-policies</c> → 201 + poliçe) baştan sona.
/// Bağımlılık: ön koşul müşterisi <c>musteri-ekle</c> (POST) ile kurulur.
/// </summary>
[Collection(Koleksiyon.Motor)]
public sealed class MotorEkleTests(EntegrasyonFixture fixture)
{
    /// <summary>
    /// MO1. Kurulum: müşteri (N). İstek: AD §6 değerleriyle dolu gövde (<see cref="TestVerisi.Motor"/>).
    /// Cevap: 201 (AD:43); alanlar gönderilenle aynı (AD §4.2-4.3); <c>policyNumber</c> tam sayı ve ≥ 1000001 (DS:49-50);
    /// <c>customerNumber</c> = N; <c>lastChanged</c> boş olmayan metin (AD:256; ADR 0003 :141-142).
    /// Veritabanı: <c>policy</c> P için tam 1 satır: customer_number = N, policy_type = 'M' (AD:136), issue_date,
    /// expiry_date, broker_id, brokers_reference, payment gönderilen değerler, last_changed NOT NULL (DS:58).
    /// <c>motor</c> P için tam 1 satır: make, model, value, reg_number, colour, cc, year_of_manufacture, premium,
    /// accidents gönderilen değerler. İki tablonun birlikte oluşması FR-00b ve NFR-04'ün olumlu yönüdür (ADR 0003 Karar 2).
    /// Doğrulanmayanlar: policy.commission (API'de yok), last_changed'in değeri ve biçimi.
    /// </summary>
    [Fact]
    [Trait("Operasyon", "motor-ekle")]
    public async Task Motor_ekle_policy_ve_motor_satirlarini_birlikte_olusturur()
    {
        var n = await fixture.Istemci.MusteriKurAsync();
        var gonderilen = TestVerisi.Motor();

        using var cevap = await fixture.Istemci.PostAsJsonAsync(
            ApiYardimcilari.PoliceAdresi(PoliceTuru.Motor, n), gonderilen);

        await cevap.DurumKoduDogrulaAsync(HttpStatusCode.Created);
        var govde = await cevap.JsonOkuAsync();
        var p = ApiYardimcilari.NumaraOku(govde, "policyNumber");
        Assert.True(p >= 1000001, $"policyNumber 1000001'den küçük: {p} (DS:49-50)");
        ApiYardimcilari.AlanlariDogrula(govde, PoliceBeklentileri.Cevap(gonderilen, n, p), "Poliçe alanları farklı");
        ApiYardimcilari.LastChangedOku(govde);

        var policy = await fixture.Veritabani.TekSatirOkuAsync(Sema.Policy.Tablo, Sema.Policy.PolicyNumber, p);
        VeritabaniYardimcilari.SatirDogrula(
            policy, PoliceBeklentileri.PolicySatiri(gonderilen, n, PoliceTuru.Motor), Sema.Policy.Tablo);
        await fixture.Veritabani.LastChangedBosDegilAsync(p);

        var motor = await fixture.Veritabani.TekSatirOkuAsync(Sema.Motor.Tablo, Sema.Motor.PolicyNumber, p);
        VeritabaniYardimcilari.SatirDogrula(motor, MotorBeklentileri.Satir(gonderilen), Sema.Motor.Tablo);
    }

    /// <summary>
    /// MO6. İstek: atanmayan müşteri 999999999'a motor poliçesi (DS:49-50; ADR 0005 02-02).
    /// Cevap: 404, <c>type: customer-not-found</c>, <c>returnCode: "70"</c> (AD:96).
    /// Veritabanı: <c>policy.customer_number = 999999999</c> için 0 satır: var olmayan müşteriye poliçe eklenemez
    /// (SRS 3.2 :122, NFR-06) ve başarısız işlem hiç satır bırakmaz (NFR-04).
    /// </summary>
    [Fact]
    [Trait("Operasyon", "motor-ekle")]
    public async Task Motor_ekle_olmayan_musteri_70_doner_ve_satir_olusturmaz()
    {
        const int atanmayanNumara = 999999999;

        using var cevap = await fixture.Istemci.PostAsJsonAsync(
            ApiYardimcilari.PoliceAdresi(PoliceTuru.Motor, atanmayanNumara), TestVerisi.Motor());

        await cevap.HataGovdesiDogrulaAsync(HttpStatusCode.NotFound, "customer-not-found", "70");
        Assert.Equal(0, await fixture.Veritabani.SayAsync(
            Sema.Policy.Tablo, (Sema.Policy.CustomerNumber, atanmayanNumara)));
    }
}
