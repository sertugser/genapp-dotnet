using System.Net;
using System.Net.Http.Json;
using GenApp.IntegrationTests.Altyapi;

namespace GenApp.IntegrationTests.Konut;

/// <summary>
/// Konut poliçesi ekle (FR-08; AD:52, <c>POST /api/customers/{c}/house-policies</c> → 201 + poliçe) baştan sona.
/// Bağımlılık: ön koşul müşterisi <c>musteri-ekle</c> (POST) ile kurulur.
/// </summary>
[Collection(Koleksiyon.Konut)]
public sealed class KonutEkleTests(EntegrasyonFixture fixture)
{
    /// <summary>
    /// K1. Kurulum: müşteri (N). İstek: boş metin içermeyen dolu gövde (<see cref="TestVerisi.Konut"/>).
    /// Cevap: 201 (AD:52); alanlar gönderilenle aynı (AD §4.2, §4.4); <c>policyNumber</c> tam sayı ve ≥ 1000001
    /// (DS:49-50); <c>customerNumber</c> = N; <c>lastChanged</c> boş olmayan metin (AD:256; ADR 0003 :141-142).
    /// Veritabanı: <c>policy</c> P için tam 1 satır: customer_number = N, policy_type = 'H' (AD:136), issue_date,
    /// expiry_date, broker_id, brokers_reference, payment gönderilen değerler, last_changed NOT NULL (DS:58).
    /// <c>house</c> P için tam 1 satır: property_type, bedrooms, value, house_name, house_number, postcode gönderilen
    /// değerler. İki tablonun birlikte oluşması FR-00b ve NFR-04'ün olumlu yönüdür (ADR 0003 Karar 2).
    /// Doğrulanmayanlar: policy.commission, last_changed'in değeri ve biçimi.
    /// </summary>
    [Fact]
    [Trait("Operasyon", "konut-ekle")]
    public async Task Konut_ekle_policy_ve_house_satirlarini_birlikte_olusturur()
    {
        var n = await fixture.Istemci.MusteriKurAsync();
        var gonderilen = TestVerisi.Konut();

        using var cevap = await fixture.Istemci.PostAsJsonAsync(
            ApiYardimcilari.PoliceAdresi(PoliceTuru.Konut, n), gonderilen);

        await cevap.DurumKoduDogrulaAsync(HttpStatusCode.Created);
        var govde = await cevap.JsonOkuAsync();
        var p = ApiYardimcilari.NumaraOku(govde, "policyNumber");
        Assert.True(p >= 1000001, $"policyNumber 1000001'den küçük: {p} (DS:49-50)");
        ApiYardimcilari.AlanlariDogrula(govde, PoliceBeklentileri.Cevap(gonderilen, n, p), "Poliçe alanları farklı");
        ApiYardimcilari.LastChangedOku(govde);

        var policy = await fixture.Veritabani.TekSatirOkuAsync(Sema.Policy.Tablo, Sema.Policy.PolicyNumber, p);
        VeritabaniYardimcilari.SatirDogrula(
            policy, PoliceBeklentileri.PolicySatiri(gonderilen, n, PoliceTuru.Konut), Sema.Policy.Tablo);
        await fixture.Veritabani.LastChangedBosDegilAsync(p);

        var house = await fixture.Veritabani.TekSatirOkuAsync(Sema.House.Tablo, Sema.House.PolicyNumber, p);
        VeritabaniYardimcilari.SatirDogrula(house, KonutBeklentileri.Satir(gonderilen), Sema.House.Tablo);
    }

    /// <summary>
    /// K5. İstek: atanmayan müşteri 999999999'a konut poliçesi (DS:49-50; ADR 0005 02-02).
    /// Cevap: 404, <c>type: customer-not-found</c>, <c>returnCode: "70"</c> (AD:96).
    /// Veritabanı: <c>policy.customer_number = 999999999</c> için 0 satır (SRS 3.2 :122, NFR-06; NFR-04).
    /// </summary>
    [Fact]
    [Trait("Operasyon", "konut-ekle")]
    public async Task Konut_ekle_olmayan_musteri_70_doner_ve_satir_olusturmaz()
    {
        const int atanmayanNumara = 999999999;

        using var cevap = await fixture.Istemci.PostAsJsonAsync(
            ApiYardimcilari.PoliceAdresi(PoliceTuru.Konut, atanmayanNumara), TestVerisi.Konut());

        await cevap.HataGovdesiDogrulaAsync(HttpStatusCode.NotFound, "customer-not-found", "70");
        Assert.Equal(0, await fixture.Veritabani.SayAsync(
            Sema.Policy.Tablo, (Sema.Policy.CustomerNumber, atanmayanNumara)));
    }
}
