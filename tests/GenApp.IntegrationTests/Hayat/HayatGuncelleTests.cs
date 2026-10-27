using System.Net;
using System.Net.Http.Json;
using GenApp.IntegrationTests.Altyapi;

namespace GenApp.IntegrationTests.Hayat;

/// <summary>
/// Hayat sigortası poliçesi güncelle (FR-14; AD:63, <c>PUT /api/customers/{c}/endowment-policies/{p}</c> → 200 + poliçe)
/// baştan sona. Bağımlılık: ön koşullar <c>musteri-ekle</c>, <c>hayat-ekle</c> (POST) ve <c>hayat-sorgula</c> (GET;
/// güncelleme sorgulamada dönen <c>lastChanged</c> ile yapılır, AD:23, AD §5) ile kurulur.
/// </summary>
[Collection(Koleksiyon.Hayat)]
public sealed class HayatGuncelleTests(EntegrasyonFixture fixture)
{
    /// <summary>
    /// H3. Kurulum: müşteri (N), hayat poliçesi (P), sorgulama (L). İstek: L ve her alanı farklı yeni değerler
    /// (<c>payment</c> dahil).
    /// Cevap: 200 (AD:63); alanlar yeni değerler, yalnızca <c>payment</c> eklemedeki değer (AD:138 "Poliçe güncelle:
    /// payment", türden bağımsız: istekteki payment yok sayılır, cevapta kayıttaki değer döner); <c>lastChanged</c> ≠ L
    /// (ADR 0003 :141-142).
    /// Veritabanı: <c>endowment</c> P satırı yeni değerlerde (sütunlar H1'deki gibi). <c>policy</c> P satırı:
    /// issue_date, expiry_date, broker_id, brokers_reference yeni; payment eklemedeki değer (AD:138); last_changed boş
    /// değil ve güncellemeden önceki değerden farklı (ADR 0003 :131-142; biçim karşılaştırılmaz).
    /// </summary>
    [Fact]
    [Trait("Operasyon", "hayat-guncelle")]
    public async Task Hayat_guncelle_degerleri_yazar_payment_degismez_last_changed_yenilenir()
    {
        var n = await fixture.Istemci.MusteriKurAsync();
        var eklenen = TestVerisi.Hayat();
        var (p, _) = await fixture.Istemci.PoliceKurAsync(PoliceTuru.Hayat, n, eklenen);
        var l = await fixture.Istemci.LastChangedSorgulaAsync(PoliceTuru.Hayat, n, p);
        var oncekiLastChanged = await fixture.Veritabani.LastChangedBosDegilAsync(p);
        var yeni = TestVerisi.HayatYeni();

        using var cevap = await fixture.Istemci.PutAsJsonAsync(
            ApiYardimcilari.PoliceAdresi(PoliceTuru.Hayat, n, p), TestVerisi.GuncellemeGovdesi(yeni, l));

        await cevap.DurumKoduDogrulaAsync(HttpStatusCode.OK);
        var govde = await cevap.JsonOkuAsync();
        var beklenenCevap = PoliceBeklentileri.Cevap(yeni, n, p);
        beklenenCevap["payment"] = eklenen["payment"]; // AD:138
        ApiYardimcilari.AlanlariDogrula(govde, beklenenCevap, "Poliçe alanları farklı");
        Assert.NotEqual(l, ApiYardimcilari.LastChangedOku(govde));

        var endowment = await fixture.Veritabani.TekSatirOkuAsync(
            Sema.Endowment.Tablo, Sema.Endowment.PolicyNumber, p);
        VeritabaniYardimcilari.SatirDogrula(endowment, HayatBeklentileri.Satir(yeni), Sema.Endowment.Tablo);

        var policy = await fixture.Veritabani.TekSatirOkuAsync(Sema.Policy.Tablo, Sema.Policy.PolicyNumber, p);
        var beklenenPolicy = PoliceBeklentileri.PolicySatiri(yeni, n, PoliceTuru.Hayat);
        beklenenPolicy[Sema.Policy.Payment] = eklenen["payment"]; // AD:138
        VeritabaniYardimcilari.SatirDogrula(policy, beklenenPolicy, Sema.Policy.Tablo);
        Assert.NotEqual(oncekiLastChanged, await fixture.Veritabani.LastChangedBosDegilAsync(p));
    }
}
