using System.Net;
using System.Net.Http.Json;
using GenApp.IntegrationTests.Altyapi;

namespace GenApp.IntegrationTests.Konut;

/// <summary>
/// Konut poliçesi güncelle (FR-10; AD:54, <c>PUT /api/customers/{c}/house-policies/{p}</c> → 200 + poliçe) baştan sona.
/// Bağımlılık: ön koşullar <c>musteri-ekle</c>, <c>konut-ekle</c> (POST) ve <c>konut-sorgula</c> (GET; güncelleme
/// sorgulamada dönen <c>lastChanged</c> ile yapılır, AD:23, AD §5) ile kurulur.
/// </summary>
[Collection(Koleksiyon.Konut)]
public sealed class KonutGuncelleTests(EntegrasyonFixture fixture)
{
    /// <summary>
    /// K3. Kurulum: müşteri (N), konut poliçesi (P), sorgulama (L). İstek: L ve her alanı farklı yeni değerler
    /// (<c>payment</c> dahil).
    /// Cevap: 200 (AD:54); alanlar yeni değerler, yalnızca <c>payment</c> eklemedeki değer (AD:138 "Poliçe güncelle:
    /// payment", türden bağımsız: istekteki payment yok sayılır, cevapta kayıttaki değer döner); <c>lastChanged</c> ≠ L
    /// (ADR 0003 :141-142).
    /// Veritabanı: <c>house</c> P satırı yeni değerlerde (sütunlar K1'deki gibi). <c>policy</c> P satırı: issue_date,
    /// expiry_date, broker_id, brokers_reference yeni; payment eklemedeki değer (AD:138); last_changed boş değil ve
    /// güncellemeden önceki değerden farklı (ADR 0003 :131-142; biçim karşılaştırılmaz).
    /// </summary>
    [Fact]
    [Trait("Operasyon", "konut-guncelle")]
    public async Task Konut_guncelle_degerleri_yazar_payment_degismez_last_changed_yenilenir()
    {
        var n = await fixture.Istemci.MusteriKurAsync();
        var eklenen = TestVerisi.Konut();
        var (p, _) = await fixture.Istemci.PoliceKurAsync(PoliceTuru.Konut, n, eklenen);
        var l = await fixture.Istemci.LastChangedSorgulaAsync(PoliceTuru.Konut, n, p);
        var oncekiLastChanged = await fixture.Veritabani.LastChangedBosDegilAsync(p);
        var yeni = TestVerisi.KonutYeni();

        using var cevap = await fixture.Istemci.PutAsJsonAsync(
            ApiYardimcilari.PoliceAdresi(PoliceTuru.Konut, n, p), TestVerisi.GuncellemeGovdesi(yeni, l));

        await cevap.DurumKoduDogrulaAsync(HttpStatusCode.OK);
        var govde = await cevap.JsonOkuAsync();
        var beklenenCevap = PoliceBeklentileri.Cevap(yeni, n, p);
        beklenenCevap["payment"] = eklenen["payment"]; // AD:138
        ApiYardimcilari.AlanlariDogrula(govde, beklenenCevap, "Poliçe alanları farklı");
        Assert.NotEqual(l, ApiYardimcilari.LastChangedOku(govde));

        var house = await fixture.Veritabani.TekSatirOkuAsync(Sema.House.Tablo, Sema.House.PolicyNumber, p);
        VeritabaniYardimcilari.SatirDogrula(house, KonutBeklentileri.Satir(yeni), Sema.House.Tablo);

        var policy = await fixture.Veritabani.TekSatirOkuAsync(Sema.Policy.Tablo, Sema.Policy.PolicyNumber, p);
        var beklenenPolicy = PoliceBeklentileri.PolicySatiri(yeni, n, PoliceTuru.Konut);
        beklenenPolicy[Sema.Policy.Payment] = eklenen["payment"]; // AD:138
        VeritabaniYardimcilari.SatirDogrula(policy, beklenenPolicy, Sema.Policy.Tablo);
        Assert.NotEqual(oncekiLastChanged, await fixture.Veritabani.LastChangedBosDegilAsync(p));
    }
}
