using System.Net;
using System.Net.Http.Json;
using GenApp.IntegrationTests.Altyapi;

namespace GenApp.IntegrationTests.Musteri;

/// <summary>
/// Müşteri ekle (FR-01; AD:35, <c>POST /api/customers</c> → 201 + müşteri) baştan sona: HTTP → API → PostgreSQL.
/// Bağımlılık yok: testler yalnızca bu operasyonu kullanır.
/// </summary>
[Collection(Koleksiyon.Musteri)]
public sealed class MusteriEkleTests(EntegrasyonFixture fixture)
{
    /// <summary>
    /// M1. Cevap: 201 (AD:35); alanlar gönderilenle aynı (AD §4.1), <c>postcode</c> büyük harf (AD:19);
    /// <c>customerNumber</c> tam sayı (AD:16) ve ≥ 1000001 (DS:49-50).
    /// Veritabanı: <c>customer</c> tablosunda N için tam 1 satır; first_name, last_name, date_of_birth, house_name,
    /// house_number, postcode (büyük harf, AD:19 "kaydedilir"), phone_mobile, phone_home, email_address gönderilen değerler.
    /// <c>customer_secure</c> tablosunda N için tam 1 satır (SRS FR-01 :98 "güvenlik kaydı da oluşturulur"; yalnızca
    /// varlık). İki tablonun birlikte oluşması FR-00b ve NFR-04'ün olumlu yönüdür.
    /// Doğrulanmayanlar: customer_secure'un değerleri (parola, state_indicator, pass_changes; belgede tanımsız),
    /// numaranın değeri (sayaçlar sıfırlanmaz, DS:52).
    /// </summary>
    [Fact]
    [Trait("Operasyon", "musteri-ekle")]
    public async Task Musteri_ekle_customer_ve_customer_secure_satirlarini_birlikte_olusturur()
    {
        var gonderilen = TestVerisi.Musteri();
        gonderilen["postcode"] = MusteriBeklentileri.KucukHarfliPostaKodu();

        using var cevap = await fixture.Istemci.PostAsJsonAsync(ApiYardimcilari.MusteriAdresi, gonderilen);

        await cevap.DurumKoduDogrulaAsync(HttpStatusCode.Created);
        var govde = await cevap.JsonOkuAsync();
        ApiYardimcilari.AlanlariDogrula(govde, MusteriBeklentileri.Cevap(gonderilen), "Müşteri alanları farklı");
        var n = ApiYardimcilari.NumaraOku(govde, "customerNumber");
        Assert.True(n >= 1000001, $"customerNumber 1000001'den küçük: {n} (DS:49-50)");

        var satir = await fixture.Veritabani.TekSatirOkuAsync(Sema.Customer.Tablo, Sema.Customer.CustomerNumber, n);
        VeritabaniYardimcilari.SatirDogrula(satir, MusteriBeklentileri.Satir(gonderilen), Sema.Customer.Tablo);
        await fixture.Veritabani.TekSatirOkuAsync(
            Sema.CustomerSecure.Tablo, Sema.CustomerSecure.CustomerNumber, n);
    }

    /// <summary>
    /// M5. İstekte gönderilen <c>customerNumber</c> yok sayılır, istek reddedilmez (AD:21).
    /// Cevap: 201; <c>customerNumber</c> gönderilen 999999998 değil.
    /// Veritabanı: <c>customer.customer_number = 999999998</c> için 0 satır; yeni numara N için tam 1 satır.
    /// 999999998, diğer testlerin "atanmayan numara" olarak kullandığı 999999999'u kirletmemek için seçildi.
    /// </summary>
    [Fact]
    [Trait("Operasyon", "musteri-ekle")]
    public async Task Musteri_ekle_istekteki_customerNumber_yok_sayilir_ve_o_numarayla_satir_olusmaz()
    {
        const int gonderilenNumara = 999999998;
        var gonderilen = TestVerisi.Musteri();
        gonderilen["customerNumber"] = gonderilenNumara;

        using var cevap = await fixture.Istemci.PostAsJsonAsync(ApiYardimcilari.MusteriAdresi, gonderilen);

        await cevap.DurumKoduDogrulaAsync(HttpStatusCode.Created);
        var n = ApiYardimcilari.NumaraOku(await cevap.JsonOkuAsync(), "customerNumber");
        Assert.NotEqual(gonderilenNumara, n);

        Assert.Equal(0, await fixture.Veritabani.SayAsync(
            Sema.Customer.Tablo, (Sema.Customer.CustomerNumber, gonderilenNumara)));
        await fixture.Veritabani.TekSatirOkuAsync(Sema.Customer.Tablo, Sema.Customer.CustomerNumber, n);
    }
}
