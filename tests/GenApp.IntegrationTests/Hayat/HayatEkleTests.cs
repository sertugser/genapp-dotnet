using System.Net;
using System.Net.Http.Json;
using GenApp.IntegrationTests.Altyapi;

namespace GenApp.IntegrationTests.Hayat;

/// <summary>
/// Hayat sigortası poliçesi ekle (FR-12; AD:61, <c>POST /api/customers/{c}/endowment-policies</c> → 201 + poliçe)
/// baştan sona. Bağımlılık: ön koşul müşterisi <c>musteri-ekle</c> (POST) ile kurulur.
/// </summary>
[Collection(Koleksiyon.Hayat)]
public sealed class HayatEkleTests(EntegrasyonFixture fixture)
{
    /// <summary>
    /// H1. Kurulum: müşteri (N). İstek: boş metin içermeyen dolu gövde, tek karakterlik alanlar "Y"/"N"
    /// (<see cref="TestVerisi.Hayat"/>).
    /// Cevap: 201 (AD:61); alanlar gönderilenle aynı (AD §4.2, §4.5); <c>policyNumber</c> tam sayı ve ≥ 1000001
    /// (DS:49-50); <c>customerNumber</c> = N; <c>lastChanged</c> boş olmayan metin (AD:256; ADR 0003 :141-142).
    /// Veritabanı: <c>policy</c> P için tam 1 satır: customer_number = N, policy_type = 'E' (AD:136), issue_date,
    /// expiry_date, broker_id, brokers_reference, payment gönderilen değerler, last_changed NOT NULL (DS:58).
    /// <c>endowment</c> P için tam 1 satır: with_profits, equities, managed_fund, fund_name, term, sum_assured,
    /// life_assured gönderilen değerler. İki tablonun birlikte oluşması FR-00b ve NFR-04'ün olumlu yönüdür
    /// (ADR 0003 Karar 2).
    /// Doğrulanmayanlar: endowment.padding_data, policy.commission, last_changed'in değeri ve biçimi.
    /// </summary>
    [Fact]
    [Trait("Operasyon", "hayat-ekle")]
    public async Task Hayat_ekle_policy_ve_endowment_satirlarini_birlikte_olusturur()
    {
        var n = await fixture.Istemci.MusteriKurAsync();
        var gonderilen = TestVerisi.Hayat();

        using var cevap = await fixture.Istemci.PostAsJsonAsync(
            ApiYardimcilari.PoliceAdresi(PoliceTuru.Hayat, n), gonderilen);

        await cevap.DurumKoduDogrulaAsync(HttpStatusCode.Created);
        var govde = await cevap.JsonOkuAsync();
        var p = ApiYardimcilari.NumaraOku(govde, "policyNumber");
        Assert.True(p >= 1000001, $"policyNumber 1000001'den küçük: {p} (DS:49-50)");
        ApiYardimcilari.AlanlariDogrula(govde, PoliceBeklentileri.Cevap(gonderilen, n, p), "Poliçe alanları farklı");
        ApiYardimcilari.LastChangedOku(govde);

        var policy = await fixture.Veritabani.TekSatirOkuAsync(Sema.Policy.Tablo, Sema.Policy.PolicyNumber, p);
        VeritabaniYardimcilari.SatirDogrula(
            policy, PoliceBeklentileri.PolicySatiri(gonderilen, n, PoliceTuru.Hayat), Sema.Policy.Tablo);
        await fixture.Veritabani.LastChangedBosDegilAsync(p);

        var endowment = await fixture.Veritabani.TekSatirOkuAsync(
            Sema.Endowment.Tablo, Sema.Endowment.PolicyNumber, p);
        VeritabaniYardimcilari.SatirDogrula(endowment, HayatBeklentileri.Satir(gonderilen), Sema.Endowment.Tablo);
    }

    /// <summary>
    /// H5. İstek: atanmayan müşteri 999999999'a hayat poliçesi (DS:49-50; ADR 0005 02-02).
    /// Cevap: 404, <c>type: customer-not-found</c>, <c>returnCode: "70"</c> (AD:96).
    /// Veritabanı: <c>policy.customer_number = 999999999</c> için 0 satır (SRS 3.2 :122, NFR-06; NFR-04).
    /// </summary>
    [Fact]
    [Trait("Operasyon", "hayat-ekle")]
    public async Task Hayat_ekle_olmayan_musteri_70_doner_ve_satir_olusturmaz()
    {
        const int atanmayanNumara = 999999999;

        using var cevap = await fixture.Istemci.PostAsJsonAsync(
            ApiYardimcilari.PoliceAdresi(PoliceTuru.Hayat, atanmayanNumara), TestVerisi.Hayat());

        await cevap.HataGovdesiDogrulaAsync(HttpStatusCode.NotFound, "customer-not-found", "70");
        Assert.Equal(0, await fixture.Veritabani.SayAsync(
            Sema.Policy.Tablo, (Sema.Policy.CustomerNumber, atanmayanNumara)));
    }
}
