using System.Net;
using System.Net.Http.Json;
using GenApp.IntegrationTests.Altyapi;

namespace GenApp.IntegrationTests.Musteri;

/// <summary>
/// Müşteri güncelle (FR-03; AD:37, <c>PUT /api/customers/{customerNumber}</c> → 200 + müşteri) baştan sona.
/// Bağımlılık: ön koşul müşterisi <c>musteri-ekle</c> (POST) ile kurulur. Müşteri güncellemesinde eşzamanlılık kontrolü
/// yoktur (AD:261-262).
/// </summary>
[Collection(Koleksiyon.Musteri)]
public sealed class MusteriGuncelleTests(EntegrasyonFixture fixture)
{
    /// <summary>
    /// M3. Kurulum: müşteri POST ile eklenir (N). İstek: her alanı farklı yeni değerler, <c>postcode</c> küçük harf.
    /// Cevap: 200 (AD:37); <c>customerNumber</c> = N, alanlar yeni değerler, <c>postcode</c> büyük harf (AD:19
    /// "ekle ve güncellede").
    /// Veritabanı: <c>customer</c> N satırı yeni değerlerde (sütunlar M1'deki gibi); <c>customer_secure</c> N için hâlâ
    /// tam 1 satır (güncelleme ikinci bir güvenlik kaydı oluşturmaz ya da onu silmez; yalnızca varlık).
    /// </summary>
    [Fact]
    [Trait("Operasyon", "musteri-guncelle")]
    public async Task Musteri_guncelle_yeni_degerleri_veritabanina_yazar()
    {
        var n = await fixture.Istemci.MusteriKurAsync();
        var yeni = TestVerisi.MusteriYeni();
        yeni["postcode"] = MusteriBeklentileri.KucukHarfliPostaKodu();

        using var cevap = await fixture.Istemci.PutAsJsonAsync(ApiYardimcilari.MusteriAdresiNumarayla(n), yeni);

        await cevap.DurumKoduDogrulaAsync(HttpStatusCode.OK);
        var beklenen = MusteriBeklentileri.Cevap(yeni);
        beklenen["customerNumber"] = n;
        ApiYardimcilari.AlanlariDogrula(await cevap.JsonOkuAsync(), beklenen, "Müşteri alanları farklı");

        var satir = await fixture.Veritabani.TekSatirOkuAsync(Sema.Customer.Tablo, Sema.Customer.CustomerNumber, n);
        VeritabaniYardimcilari.SatirDogrula(satir, MusteriBeklentileri.Satir(yeni), Sema.Customer.Tablo);
        await fixture.Veritabani.TekSatirOkuAsync(
            Sema.CustomerSecure.Tablo, Sema.CustomerSecure.CustomerNumber, n);
    }

    /// <summary>
    /// M4. İstek: atanmayan 999999999 numarasına PUT (DS:49-50; ADR 0005 02-02).
    /// Cevap: 404, <c>type: not-found</c>, <c>returnCode: "01"</c> (AD:132 "Müşteri güncelle: müşteri yok"; eşleme AD:94).
    /// Veritabanı: <c>customer.customer_number = 999999999</c> için 0 satır (PUT kayıt oluşturmaz).
    /// </summary>
    [Fact]
    [Trait("Operasyon", "musteri-guncelle")]
    public async Task Musteri_guncelle_olmayan_musteri_404_doner_ve_satir_olusturmaz()
    {
        const int atanmayanNumara = 999999999;

        using var cevap = await fixture.Istemci.PutAsJsonAsync(
            ApiYardimcilari.MusteriAdresiNumarayla(atanmayanNumara), TestVerisi.MusteriYeni());

        await cevap.HataGovdesiDogrulaAsync(HttpStatusCode.NotFound, "not-found", "01");
        Assert.Equal(0, await fixture.Veritabani.SayAsync(
            Sema.Customer.Tablo, (Sema.Customer.CustomerNumber, atanmayanNumara)));
    }
}
