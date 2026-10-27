using System.Net;
using GenApp.IntegrationTests.Altyapi;

namespace GenApp.IntegrationTests.Ticari;

/// <summary>
/// Ticari poliçe sorgula (FR-17), dört biçim (AD §2.5): 17a müşteri ve poliçe numarasıyla (AD:71), 17b yalnızca poliçe
/// numarasıyla (AD:72), 17c müşteri numarasıyla liste (AD:73), 17d posta koduyla liste (AD:74). Hepsi
/// <c>ticari-sorgula</c> etiketini taşır (AD:79-82).
/// Bağımlılık: ön koşullar <c>musteri-ekle</c> ve <c>ticari-ekle</c> (POST) ile kurulur.
/// </summary>
/// <remarks>
/// Listelerin sırası tanımsızdır; poliçe numaraları küme olarak karşılaştırılır. Liste testleri her çalıştırmada yeni
/// müşteri ve teste özgü posta kodu (Z) kullanır; bu yüzden "tam N öğe" beklentisi başka testlerin verisinden etkilenmez.
/// </remarks>
[Collection(Koleksiyon.Ticari)]
public sealed class TicariSorgulaTests(EntegrasyonFixture fixture)
{
    /// <summary>
    /// T2 (17a). Kurulum: müşteri (N) ve ticari poliçe (P).
    /// Cevap: 200 (AD:71); alanlar eklenen değerler (AD §4.2, §4.6), <c>customerNumber</c> = N, <c>policyNumber</c> = P;
    /// <c>brokerId</c>, <c>brokersReference</c>, <c>payment</c> kayıttaki (eklenen) değerler (AD:142).
    /// Veritabanı: T1'de doğrulandı.
    /// </summary>
    [Fact]
    [Trait("Operasyon", "ticari-sorgula")]
    public async Task Ticari_sorgula_musteri_ve_police_numarasiyla_eklenen_policeyi_doner()
    {
        var n = await fixture.Istemci.MusteriKurAsync();
        var eklenen = TestVerisi.Ticari(TestVerisi.PostaKodu());
        var (p, _) = await fixture.Istemci.PoliceKurAsync(PoliceTuru.Ticari, n, eklenen);

        using var cevap = await fixture.Istemci.GetAsync(ApiYardimcilari.PoliceAdresi(PoliceTuru.Ticari, n, p));

        await cevap.DurumKoduDogrulaAsync(HttpStatusCode.OK);
        ApiYardimcilari.AlanlariDogrula(
            await cevap.JsonOkuAsync(), PoliceBeklentileri.Cevap(eklenen, n, p), "Poliçe alanları farklı");
    }

    /// <summary>
    /// T3 (17b). Kurulum: müşteri (N) ve ticari poliçe (P). İstek: <c>GET /api/commercial-policies/{P}</c>.
    /// Cevap: 200 (AD:72); <c>customerNumber</c> = N (adreste müşteri numarası olmadan), <c>policyNumber</c> = P, alanlar
    /// eklenen değerler; broker ve payment alanları kayıttaki değerler (AD:142).
    /// </summary>
    [Fact]
    [Trait("Operasyon", "ticari-sorgula")]
    public async Task Ticari_sorgula_yalnizca_police_numarasiyla_eklenen_policeyi_ve_musteri_numarasini_doner()
    {
        var n = await fixture.Istemci.MusteriKurAsync();
        var eklenen = TestVerisi.Ticari(TestVerisi.PostaKodu());
        var (p, _) = await fixture.Istemci.PoliceKurAsync(PoliceTuru.Ticari, n, eklenen);

        using var cevap = await fixture.Istemci.GetAsync(ApiYardimcilari.TicariPoliceAdresi(p));

        await cevap.DurumKoduDogrulaAsync(HttpStatusCode.OK);
        ApiYardimcilari.AlanlariDogrula(
            await cevap.JsonOkuAsync(), PoliceBeklentileri.Cevap(eklenen, n, p), "Poliçe alanları farklı");
    }

    /// <summary>
    /// T4 (17c). Kurulum: yeni müşteri (N) ve ona iki ticari poliçe (P1, P2).
    /// Cevap: 200 (AD:73); JSON dizisi, tam 2 öğe; <c>policyNumber</c> kümesi {P1, P2}: bulunan kayıtların hepsi liste
    /// olarak döner (AD:139; eski sistemdeki "yalnızca son kayıt" bilinçli olarak taşınmaz).
    /// Veritabanı: <c>policy</c> tablosunda (customer_number = N, policy_type = 'C') sayısı 2 = liste uzunluğu.
    /// </summary>
    [Fact]
    [Trait("Operasyon", "ticari-sorgula")]
    public async Task Ticari_sorgula_musteri_numarasiyla_butun_policeleri_liste_olarak_doner()
    {
        var n = await fixture.Istemci.MusteriKurAsync();
        var (p1, _) = await fixture.Istemci.PoliceKurAsync(PoliceTuru.Ticari, n, TestVerisi.Ticari(TestVerisi.PostaKodu()));
        var (p2, _) = await fixture.Istemci.PoliceKurAsync(PoliceTuru.Ticari, n, TestVerisi.Ticari(TestVerisi.PostaKodu()));

        using var cevap = await fixture.Istemci.GetAsync(ApiYardimcilari.PoliceAdresi(PoliceTuru.Ticari, n));

        await cevap.DurumKoduDogrulaAsync(HttpStatusCode.OK);
        var numaralar = ApiYardimcilari.PoliceNumaralariOku(await cevap.JsonOkuAsync());
        Assert.Equal(2, numaralar.Count);
        Assert.Equal(new HashSet<int> { p1, p2 }, numaralar.ToHashSet());
        Assert.Equal(2, await fixture.Veritabani.SayAsync(
            Sema.Policy.Tablo, (Sema.Policy.CustomerNumber, n), (Sema.Policy.PolicyType, PoliceTuru.Ticari.Harf)));
    }

    /// <summary>
    /// T5 (17c, kayıt yok). Kurulum: poliçesi olmayan yeni müşteri (N).
    /// Cevap: 200 ve boş liste <c>[]</c> (AD:140, korunan davranış: <c>00</c>'ın karşılığı).
    /// Veritabanı: <c>policy.customer_number = N</c> için 0 satır.
    /// </summary>
    [Fact]
    [Trait("Operasyon", "ticari-sorgula")]
    public async Task Ticari_sorgula_musteri_numarasiyla_kayit_yoksa_bos_liste_doner()
    {
        var n = await fixture.Istemci.MusteriKurAsync();

        using var cevap = await fixture.Istemci.GetAsync(ApiYardimcilari.PoliceAdresi(PoliceTuru.Ticari, n));

        await cevap.DurumKoduDogrulaAsync(HttpStatusCode.OK);
        Assert.Empty(ApiYardimcilari.PoliceNumaralariOku(await cevap.JsonOkuAsync()));
        Assert.Equal(0, await fixture.Veritabani.SayAsync(Sema.Policy.Tablo, (Sema.Policy.CustomerNumber, n)));
    }

    /// <summary>
    /// T6 (17d). Kurulum: iki yeni müşteri, her birine aynı teste özgü posta koduyla (Z) birer ticari poliçe (P1, P2).
    /// İstek: <c>GET /api/commercial-policies?postcode=Z</c>.
    /// Cevap: 200 (AD:74); JSON dizisi, tam 2 öğe; <c>policyNumber</c> kümesi {P1, P2} (AD:139: bütün kayıtlar).
    /// Veritabanı: <c>commercial.zipcode = Z</c> sayısı 2 (eşleme postcode → zipcode, G2; T1'deki gibi).
    /// Z büyük harflidir; ticari posta kodunun büyük harfe çevrilmesi ve sorgunun harf duyarlılığı tanımsızdır.
    /// </summary>
    [Fact]
    [Trait("Operasyon", "ticari-sorgula")]
    public async Task Ticari_sorgula_posta_koduyla_butun_policeleri_liste_olarak_doner()
    {
        var z = TestVerisi.PostaKodu();
        var n1 = await fixture.Istemci.MusteriKurAsync();
        var n2 = await fixture.Istemci.MusteriKurAsync();
        var (p1, _) = await fixture.Istemci.PoliceKurAsync(PoliceTuru.Ticari, n1, TestVerisi.Ticari(z));
        var (p2, _) = await fixture.Istemci.PoliceKurAsync(PoliceTuru.Ticari, n2, TestVerisi.Ticari(z));

        using var cevap = await fixture.Istemci.GetAsync(ApiYardimcilari.TicariPostaKoduAdresi(z));

        await cevap.DurumKoduDogrulaAsync(HttpStatusCode.OK);
        var numaralar = ApiYardimcilari.PoliceNumaralariOku(await cevap.JsonOkuAsync());
        Assert.Equal(2, numaralar.Count);
        Assert.Equal(new HashSet<int> { p1, p2 }, numaralar.ToHashSet());
        Assert.Equal(2, await fixture.Veritabani.SayAsync(Sema.Commercial.Tablo, (Sema.Commercial.Zipcode, z)));
    }

    /// <summary>
    /// T7 (17d, kayıt yok). İstek: hiç kullanılmamış, teste özgü posta kodu (Z).
    /// Cevap: 200 ve boş liste <c>[]</c> (AD:140).
    /// Veritabanı: <c>commercial.zipcode = Z</c> sayısı 0.
    /// </summary>
    [Fact]
    [Trait("Operasyon", "ticari-sorgula")]
    public async Task Ticari_sorgula_posta_koduyla_kayit_yoksa_bos_liste_doner()
    {
        var z = TestVerisi.PostaKodu();

        using var cevap = await fixture.Istemci.GetAsync(ApiYardimcilari.TicariPostaKoduAdresi(z));

        await cevap.DurumKoduDogrulaAsync(HttpStatusCode.OK);
        Assert.Empty(ApiYardimcilari.PoliceNumaralariOku(await cevap.JsonOkuAsync()));
        Assert.Equal(0, await fixture.Veritabani.SayAsync(Sema.Commercial.Tablo, (Sema.Commercial.Zipcode, z)));
    }
}
