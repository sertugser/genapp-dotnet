using System.Net;
using System.Net.Http.Json;
using GenApp.IntegrationTests.Altyapi;

namespace GenApp.IntegrationTests.Motor;

/// <summary>
/// Motor poliçesi güncelle (FR-06; AD:45, <c>PUT /api/customers/{c}/motor-policies/{p}</c> → 200 + poliçe) baştan sona.
/// Bağımlılık: ön koşullar <c>musteri-ekle</c>, <c>motor-ekle</c> (POST) ve <c>motor-sorgula</c> (GET; güncelleme
/// sorgulamada dönen <c>lastChanged</c> ile yapılır, AD:23, AD §5) ile kurulur.
/// </summary>
[Collection(Koleksiyon.Motor)]
public sealed class MotorGuncelleTests(EntegrasyonFixture fixture)
{
    /// <summary>
    /// MO3. Kurulum: müşteri (N), motor poliçesi (P), sorgulama (L). İstek: L ve her alanı farklı yeni değerler
    /// (<c>payment</c> dahil).
    /// Cevap: 200 (AD:45); alanlar yeni değerler, yalnızca <c>payment</c> eklemedeki değer (AD:138: istekteki payment yok
    /// sayılır, cevapta kayıttaki değer döner); <c>lastChanged</c> ≠ L (ADR 0003 :141-142, güncelleme yeni değeri döndürür).
    /// Veritabanı: <c>motor</c> P satırı yeni değerlerde (sütunlar MO1'deki gibi). <c>policy</c> P satırı: issue_date,
    /// expiry_date, broker_id, brokers_reference yeni; payment eklemedeki değer (AD:138); last_changed boş değil ve
    /// güncellemeden önceki değerden farklı (ADR 0003 :131-142; biçim karşılaştırılmaz).
    /// </summary>
    [Fact]
    [Trait("Operasyon", "motor-guncelle")]
    public async Task Motor_guncelle_degerleri_yazar_payment_degismez_last_changed_yenilenir()
    {
        var n = await fixture.Istemci.MusteriKurAsync();
        var eklenen = TestVerisi.Motor();
        var (p, _) = await fixture.Istemci.PoliceKurAsync(PoliceTuru.Motor, n, eklenen);
        var l = await fixture.Istemci.LastChangedSorgulaAsync(PoliceTuru.Motor, n, p);
        var oncekiLastChanged = await fixture.Veritabani.LastChangedBosDegilAsync(p);
        var yeni = TestVerisi.MotorYeni();

        using var cevap = await fixture.Istemci.PutAsJsonAsync(
            ApiYardimcilari.PoliceAdresi(PoliceTuru.Motor, n, p), TestVerisi.GuncellemeGovdesi(yeni, l));

        await cevap.DurumKoduDogrulaAsync(HttpStatusCode.OK);
        var govde = await cevap.JsonOkuAsync();
        var beklenenCevap = PoliceBeklentileri.Cevap(yeni, n, p);
        beklenenCevap["payment"] = eklenen["payment"]; // AD:138
        ApiYardimcilari.AlanlariDogrula(govde, beklenenCevap, "Poliçe alanları farklı");
        Assert.NotEqual(l, ApiYardimcilari.LastChangedOku(govde));

        var motor = await fixture.Veritabani.TekSatirOkuAsync(Sema.Motor.Tablo, Sema.Motor.PolicyNumber, p);
        VeritabaniYardimcilari.SatirDogrula(motor, MotorBeklentileri.Satir(yeni), Sema.Motor.Tablo);

        var policy = await fixture.Veritabani.TekSatirOkuAsync(Sema.Policy.Tablo, Sema.Policy.PolicyNumber, p);
        var beklenenPolicy = PoliceBeklentileri.PolicySatiri(yeni, n, PoliceTuru.Motor);
        beklenenPolicy[Sema.Policy.Payment] = eklenen["payment"]; // AD:138
        VeritabaniYardimcilari.SatirDogrula(policy, beklenenPolicy, Sema.Policy.Tablo);
        Assert.NotEqual(oncekiLastChanged, await fixture.Veritabani.LastChangedBosDegilAsync(p));
    }

    /// <summary>
    /// MO4. Kurulum: müşteri (N), motor poliçesi (P), sorgulama (L), L ile ilk güncelleme (200, yeni değerler).
    /// İstek: eski L ile ikinci güncelleme, ilk güncellemeden farklı değerlerle (eklemedeki değerler).
    /// Cevap: 409, <c>type: concurrency-conflict</c>, <c>returnCode: "02"</c> (AD:95; AD:257-258 "kayıt arada değişmişse
    /// güncelleme yapılmaz").
    /// Veritabanı: <c>motor</c> ve <c>policy</c> P satırları ilk güncellemenin değerlerinde (payment eklemedeki değer,
    /// AD:138); last_changed ilk güncellemeden sonraki değerle aynı. Son yazan öncekinin değişikliğini ezemez
    /// (SRS 3.2 :124-126, NFR-05; ADR 0003 :125-130: çakışmada hiçbir şey yazılmaz).
    /// </summary>
    [Fact]
    [Trait("Operasyon", "motor-guncelle")]
    public async Task Motor_guncelle_eski_lastChanged_ile_409_doner_ve_kayit_degismez()
    {
        var n = await fixture.Istemci.MusteriKurAsync();
        var eklenen = TestVerisi.Motor();
        var (p, _) = await fixture.Istemci.PoliceKurAsync(PoliceTuru.Motor, n, eklenen);
        var l = await fixture.Istemci.LastChangedSorgulaAsync(PoliceTuru.Motor, n, p);
        var adres = ApiYardimcilari.PoliceAdresi(PoliceTuru.Motor, n, p);

        var ilk = TestVerisi.MotorYeni();
        using (var ilkCevap = await fixture.Istemci.PutAsJsonAsync(adres, TestVerisi.GuncellemeGovdesi(ilk, l)))
        {
            await ilkCevap.DurumKoduDogrulaAsync(HttpStatusCode.OK, "Ön koşul (ilk güncelleme) kurulamadı");
        }
        var ilkGuncellemedenSonra = await fixture.Veritabani.LastChangedBosDegilAsync(p);

        using var cevap = await fixture.Istemci.PutAsJsonAsync(
            adres, TestVerisi.GuncellemeGovdesi(TestVerisi.Motor(), l));

        await cevap.HataGovdesiDogrulaAsync(HttpStatusCode.Conflict, "concurrency-conflict", "02");

        var motor = await fixture.Veritabani.TekSatirOkuAsync(Sema.Motor.Tablo, Sema.Motor.PolicyNumber, p);
        VeritabaniYardimcilari.SatirDogrula(motor, MotorBeklentileri.Satir(ilk), Sema.Motor.Tablo);

        var policy = await fixture.Veritabani.TekSatirOkuAsync(Sema.Policy.Tablo, Sema.Policy.PolicyNumber, p);
        var beklenenPolicy = PoliceBeklentileri.PolicySatiri(ilk, n, PoliceTuru.Motor);
        beklenenPolicy[Sema.Policy.Payment] = eklenen["payment"]; // AD:138
        VeritabaniYardimcilari.SatirDogrula(policy, beklenenPolicy, Sema.Policy.Tablo);
        Assert.Equal(ilkGuncellemedenSonra, await fixture.Veritabani.LastChangedBosDegilAsync(p));
    }
}
