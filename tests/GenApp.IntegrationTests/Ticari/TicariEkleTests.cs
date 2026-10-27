using System.Net;
using System.Net.Http.Json;
using GenApp.IntegrationTests.Altyapi;

namespace GenApp.IntegrationTests.Ticari;

/// <summary>
/// Ticari poliçe ekle (FR-16; AD:70, <c>POST /api/customers/{c}/commercial-policies</c> → 201 + poliçe) baştan sona.
/// Bağımlılık: ön koşul müşterisi <c>musteri-ekle</c> (POST) ile kurulur.
/// </summary>
[Collection(Koleksiyon.Ticari)]
public sealed class TicariEkleTests(EntegrasyonFixture fixture)
{
    /// <summary>
    /// T1. Kurulum: müşteri (N). İstek: 16 ticari alanı dolu, boş metin içermeyen gövde; posta kodu teste özgü
    /// (<see cref="TestVerisi.Ticari"/>).
    /// Cevap: 201 (AD:70); alanlar gönderilenle aynı, tarihler dahil (AD §4.2, §4.6); <c>policyNumber</c> tam sayı ve
    /// ≥ 1000001 (DS:49-50); <c>customerNumber</c> = N; <c>lastChanged</c> boş olmayan metin (AD:256; ADR 0003 :141-142).
    /// Veritabanı: <c>policy</c> P için tam 1 satır: customer_number = N, policy_type = 'C' (AD:136), broker_id,
    /// brokers_reference, payment gönderilen değerler, last_changed NOT NULL (DS:58). <c>commercial</c> P için tam 1
    /// satır: address, zipcode, latitude_n, longitude_w, customer, property_type, fire/crime/flood/weather peril ve
    /// premium, status, rejection_reason gönderilen değerler. İki tablonun birlikte oluşması FR-00b ve NFR-04'ün olumlu
    /// yönüdür (ADR 0003 Karar 2).
    /// Doğrulanmayanlar: tarih sütunları (policy.issue_date/expiry_date, commercial.start_date/renewal_date; eşleme
    /// tanımsız, G2), commercial.request_date, policy.commission, last_changed'in değeri ve biçimi.
    /// </summary>
    [Fact]
    [Trait("Operasyon", "ticari-ekle")]
    public async Task Ticari_ekle_policy_ve_commercial_satirlarini_birlikte_olusturur()
    {
        var n = await fixture.Istemci.MusteriKurAsync();
        var gonderilen = TestVerisi.Ticari(TestVerisi.PostaKodu());

        using var cevap = await fixture.Istemci.PostAsJsonAsync(
            ApiYardimcilari.PoliceAdresi(PoliceTuru.Ticari, n), gonderilen);

        await cevap.DurumKoduDogrulaAsync(HttpStatusCode.Created);
        var govde = await cevap.JsonOkuAsync();
        var p = ApiYardimcilari.NumaraOku(govde, "policyNumber");
        Assert.True(p >= 1000001, $"policyNumber 1000001'den küçük: {p} (DS:49-50)");
        ApiYardimcilari.AlanlariDogrula(govde, PoliceBeklentileri.Cevap(gonderilen, n, p), "Poliçe alanları farklı");
        ApiYardimcilari.LastChangedOku(govde);

        var policy = await fixture.Veritabani.TekSatirOkuAsync(Sema.Policy.Tablo, Sema.Policy.PolicyNumber, p);
        VeritabaniYardimcilari.SatirDogrula(
            policy,
            PoliceBeklentileri.PolicySatiri(gonderilen, n, PoliceTuru.Ticari, tarihlerDahil: false),
            Sema.Policy.Tablo);
        await fixture.Veritabani.LastChangedBosDegilAsync(p);

        var commercial = await fixture.Veritabani.TekSatirOkuAsync(
            Sema.Commercial.Tablo, Sema.Commercial.PolicyNumber, p);
        VeritabaniYardimcilari.SatirDogrula(commercial, TicariBeklentileri.Satir(gonderilen), Sema.Commercial.Tablo);
    }

    /// <summary>
    /// T9. İstek: atanmayan müşteri 999999999'a ticari poliçe (DS:49-50; ADR 0005 02-02).
    /// Cevap: 404, <c>type: customer-not-found</c>, <c>returnCode: "70"</c> (AD:96).
    /// Veritabanı: <c>policy.customer_number = 999999999</c> için 0 satır (SRS 3.2 :122, NFR-06; NFR-04).
    /// </summary>
    [Fact]
    [Trait("Operasyon", "ticari-ekle")]
    public async Task Ticari_ekle_olmayan_musteri_70_doner_ve_satir_olusturmaz()
    {
        const int atanmayanNumara = 999999999;

        using var cevap = await fixture.Istemci.PostAsJsonAsync(
            ApiYardimcilari.PoliceAdresi(PoliceTuru.Ticari, atanmayanNumara), TestVerisi.Ticari(TestVerisi.PostaKodu()));

        await cevap.HataGovdesiDogrulaAsync(HttpStatusCode.NotFound, "customer-not-found", "70");
        Assert.Equal(0, await fixture.Veritabani.SayAsync(
            Sema.Policy.Tablo, (Sema.Policy.CustomerNumber, atanmayanNumara)));
    }
}
