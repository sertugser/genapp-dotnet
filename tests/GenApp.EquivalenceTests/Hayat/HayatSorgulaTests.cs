using System.Net;
using GenApp.EquivalenceTests.Altyapi;

namespace GenApp.EquivalenceTests.Hayat;

/// <summary>
/// Hayat sigortası poliçesi sorgula: <c>GET /api/customers/{customerNumber}/endowment-policies/{policyNumber}</c> (eski
/// program LGIPOL01 → LGIPDB01). Tahminler: tests/predictions/13-endowment-inquire.md. Beklenen cevaplar:
/// docs/api-design.md §2.4, §3, §4.2, §4.5.
/// Her test kendi müşterisini POST ile kurar (musteri-ekle). Kurulum için hayat sigortası poliçesi de POST ile kurulur; bu
/// testler hayat-ekle etkin değilken kırmızı olur. Tek istisna 13-03'tür: yalnızca müşteri POST eder. Etkinleştirme sırası:
/// hayat-ekle, hayat-sorgula, hayat-guncelle, hayat-sil.
/// </summary>
public sealed class HayatSorgulaTests : IClassFixture<GenAppApiFactory>
{
    private readonly HttpClient _istemci;

    public HayatSorgulaTests(GenAppApiFactory fabrika) => _istemci = fabrika.CreateClient();

    // Tahmin 13-01, lgipdb01.cbl:370 (ENDOWMENT SELECT'i SQLCODE = 0; CA-RETURN-CODE :258'deki '00' olarak kalır, veri
    // :407-409'da COMMAREA'ya taşınır). Poliçe 4'e güvenilmez: örnek verinin test veritabanına yüklü olduğu garanti değil.
    // Bu yüzden poliçe 4'ün değerleriyle (HayatVerisi.Police4) POST edilen poliçe sorgulanır. Tahmindeki B4 ön koşulu
    // (düzenek SELECT'in host değişkenlerini poliçenin Db2 değerleriyle doldurur) gerçek veritabanında değerlerin satırdan
    // gelmesine karşılık gelir.
    // Beklenen: 200 + poliçe (api-design.md §2.4, §3); policyNumber ve customerNumber adresteki numaralar; alanlar POST
    // edilen değerler: tarihler yyyy-MM-dd (§1), metinler kırpılmış (ADR 0002 Karar 6); lastChanged boş olmayan bir
    // metin (§5: sorgulamanın cevabında döner).
    // Doğrulanmayanlar: brokersReference (boş gönderilir, boş metin beklentisi yazılmaz; HayatVerisi), lastChanged'in
    // biçimi (sunucu belirler, §5; eski sistemde de belirsiz: 13-01), 'FINAL' işareti (:417; COMMAREA'ya ait, API'de
    // karşılığı yok), paddingData (API'de yok, §4.5).
    [Fact]
    [Trait("Operasyon", "hayat-sorgula")]
    public async Task LGIPDB01_satir_370_hayat_police_var_00_doner()
    {
        var musteriNumarasi = await _istemci.MusteriKurAsync(MusteriVerisi.AndrewPandy);
        var policeNumarasi = await _istemci.HayatPoliceKurAsync(musteriNumarasi, HayatVerisi.Police4);

        using var cevap = await _istemci.GetAsync(
            HayatYardimcilari.HayatAdresiNumarayla(musteriNumarasi, policeNumarasi));

        await cevap.DurumKoduDogrulaAsync(HttpStatusCode.OK);
        var police = await cevap.JsonOkuAsync();
        Assert.Equal(policeNumarasi, PoliceYardimcilari.PoliceNumarasiOku(police));
        Assert.Equal(musteriNumarasi, ApiYardimcilari.MusteriNumarasiOku(police));
        PoliceYardimcilari.LastChangedOku(police);
        PoliceYardimcilari.PoliceAlanlariDogrula(
            police, PoliceYardimcilari.AlanlarHaric(HayatVerisi.Police4, HayatVerisi.BosGonderilenAlanlar));
    }

    // Tahmin 13-02, lgipdb01.cbl:370 (13-01 ile aynı yol, ikinci bir veri kümesinin doğru eşlendiğini sınar). Tahmin
    // poliçe 5'i ve müşteri 3'ü kullanır; poliçe 5'in değerleriyle (HayatVerisi.Police5: bayraklar N N N, fon SHEPPA, vade
    // 10, teminat 50000, yaşam sigortalısı Shep) POST edilen poliçe sorgulanır.
    // Beklenen: 200 + poliçe (api-design.md §2.4, §3); alanlar POST edilen değerler.
    // Doğrulanmayanlar: 13-01'dekiler.
    [Fact]
    [Trait("Operasyon", "hayat-sorgula")]
    public async Task LGIPDB01_satir_370_hayat_ikinci_police_var_00_doner()
    {
        var musteriNumarasi = await _istemci.MusteriKurAsync(MusteriVerisi.AndrewPandy);
        var policeNumarasi = await _istemci.HayatPoliceKurAsync(musteriNumarasi, HayatVerisi.Police5);

        using var cevap = await _istemci.GetAsync(
            HayatYardimcilari.HayatAdresiNumarayla(musteriNumarasi, policeNumarasi));

        await cevap.DurumKoduDogrulaAsync(HttpStatusCode.OK);
        var police = await cevap.JsonOkuAsync();
        Assert.Equal(policeNumarasi, PoliceYardimcilari.PoliceNumarasiOku(police));
        Assert.Equal(musteriNumarasi, ApiYardimcilari.MusteriNumarasiOku(police));
        PoliceYardimcilari.PoliceAlanlariDogrula(
            police, PoliceYardimcilari.AlanlarHaric(HayatVerisi.Police5, HayatVerisi.BosGonderilenAlanlar));
    }

    // Tahmin 13-03, lgipdb01.cbl:423 ([Db2 bilgisi] SELECT satır bulamaz, SQLCODE = 100 (:421) → CA-RETURN-CODE '01').
    // Beklenen: 404, type "not-found", returnCode "01" (api-design.md §3).
    // Kurulum: tahmindeki müşteri 8 gibi var olan bir müşteri POST ile kurulur. Poliçe kurulmaz; bu test hayat-ekle'ye
    // bağlı değildir.
    // Numara seçimi: Tahmin poliçe 999 kullanır (var olmayan bir numara). PoliceYardimcilari.AtanmayanNumara
    // (999999999): kimlik değeri 1000001'den başlar (docs/database-schema.md) ve policyNumber istekte gönderilse de yok
    // sayılır (§1); bu numaraya testlerde pratikte ulaşılmaz. Dokuz hanelik numara DB2-POLICYNUM-INT'e (PIC S9(9) COMP,
    // lgipdb01.cbl:168) kesilmeden sığar; COBOL'da da aynı yol çalışır: satır yok, SQLCODE = 100.
    // Doğrulanmayanlar: COMMAREA'nın CA-RETURN-CODE dışında değişmemesi ve 'FINAL'in yazılmaması (API'de karşılığı yok).
    [Fact]
    [Trait("Operasyon", "hayat-sorgula")]
    public async Task LGIPDB01_satir_423_hayat_police_yok_01_doner()
    {
        var musteriNumarasi = await _istemci.MusteriKurAsync(MusteriVerisi.AndrewPandy);

        using var cevap = await _istemci.GetAsync(
            HayatYardimcilari.HayatAdresiNumarayla(musteriNumarasi, PoliceYardimcilari.AtanmayanNumara));

        await cevap.HataGovdesiDogrulaAsync(HttpStatusCode.NotFound, "not-found", "01");
    }

    // Tahmin 13-04, lgipdb01.cbl:423 (WHERE müşteri ve poliçe numarasını birlikte ister, :361-367; [Db2 bilgisi] satır yok,
    // SQLCODE = 100 → '01'). Poliçe numarası tek başına yetmez.
    // Kurulum: A müşterisi ve poliçesi (tahminde müşteri 8 ve poliçe 4) ile B müşterisi (tahminde müşteri 3) POST ile
    // kurulur; A'nın poliçesi B'nin adresinden sorgulanır.
    // Beklenen: 404, type "not-found", returnCode "01" (api-design.md §3).
    // Doğrulanmayanlar: COMMAREA'nın CA-RETURN-CODE dışında değişmemesi (API'de karşılığı yok).
    [Fact]
    [Trait("Operasyon", "hayat-sorgula")]
    public async Task LGIPDB01_satir_423_hayat_musteri_police_eslesmiyor_01_doner()
    {
        var musteriA = await _istemci.MusteriKurAsync(MusteriVerisi.AndrewPandy);
        var policeNumarasi = await _istemci.HayatPoliceKurAsync(musteriA, HayatVerisi.Police4);
        var musteriB = await _istemci.MusteriKurAsync(MusteriVerisi.AndrewPandy);

        using var cevap = await _istemci.GetAsync(HayatYardimcilari.HayatAdresiNumarayla(musteriB, policeNumarasi));

        await cevap.HataGovdesiDogrulaAsync(HttpStatusCode.NotFound, "not-found", "01");
    }

    // Tahmin 13-18, lgipdb01.cbl:408 (MOVE DB2-ENDOW-FIXED TO CA-ENDOWMENT: SELECT listesi :337-338 ve INTO listesi :352-353
    // withProfits ve equities'i aynı sırada yazar, CA-ENDOWMENT düzeni de aynı sıradadır: lgpolicy.cpy:55-56,
    // lgcmarea.cpy:47-48). Örnek veride iki bayrak hep aynıdır (Y Y ya da N N); iki alan yer değiştirse fark edilmez. Tahmin
    // bu yüzden withProfits N, equities Y kurar.
    // Kurulum: poliçe 4'ün değerleri, ama withProfits "N" ve equities "Y" (HayatVerisi.Police4FarkliBayraklarla) ile POST
    // edilir; ardından GET.
    // Beklenen: 200 + poliçe (api-design.md §2.4, §3); withProfits "N" ve equities "Y" (yer değiştirmemiş); diğer alanlar
    // POST edilen değerler.
    // Doğrulanmayanlar: 13-01'dekiler.
    [Fact]
    [Trait("Operasyon", "hayat-sorgula")]
    public async Task LGIPDB01_satir_408_hayat_bayraklar_yer_degistirmez_00_doner()
    {
        var musteriNumarasi = await _istemci.MusteriKurAsync(MusteriVerisi.AndrewPandy);
        var policeNumarasi = await _istemci.HayatPoliceKurAsync(
            musteriNumarasi, HayatVerisi.Police4FarkliBayraklarla);

        using var cevap = await _istemci.GetAsync(
            HayatYardimcilari.HayatAdresiNumarayla(musteriNumarasi, policeNumarasi));

        await cevap.DurumKoduDogrulaAsync(HttpStatusCode.OK);
        PoliceYardimcilari.PoliceAlanlariDogrula(
            await cevap.JsonOkuAsync(),
            PoliceYardimcilari.AlanlarHaric(HayatVerisi.Police4FarkliBayraklarla, HayatVerisi.BosGonderilenAlanlar));
    }
}
