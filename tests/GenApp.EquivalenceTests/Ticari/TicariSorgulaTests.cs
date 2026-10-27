using System.Net;
using System.Text.Json;
using GenApp.EquivalenceTests.Altyapi;

namespace GenApp.EquivalenceTests.Ticari;

/// <summary>
/// Ticari poliçe sorgula, dört biçim (eski program LGIPOL01 → LGIPDB01; docs/api-design.md §2.5):
/// 17a <c>GET /api/customers/{customerNumber}/commercial-policies/{policyNumber}</c> (01ICOM),
/// 17b <c>GET /api/commercial-policies/{policyNumber}</c> (02ICOM),
/// 17c <c>GET /api/customers/{customerNumber}/commercial-policies</c> (03ICOM, liste),
/// 17d <c>GET /api/commercial-policies?postcode={postcode}</c> (05ICOM, liste).
/// Tahminler: tests/predictions/17-commercial-inquire.md. Beklenen cevaplar: docs/api-design.md §2.5, §3, §4.2, §4.6.
/// Hepsinin operasyon adı <c>ticari-sorgula</c>'dır (§2.5).
/// <para>
/// Bağımlılıklar: 17-07 ve 17-24 yalnızca müşteri POST eder (musteri-ekle). 17-34 hiç kurulum yapmaz. Diğer dokuz test
/// (17-04, 17-05, 17-08, 17-16, 17-17, 17-23, 17-25, 17-33, 17-35) poliçeyi de POST ile kurar; bu testler ticari-ekle etkin
/// değilken kırmızı olur. Etkinleştirme sırası: musteri-ekle, ticari-ekle, ticari-sorgula, ticari-sil.
/// </para>
/// <para>
/// Eski sistemde liste sorguları (03ICOM, 05ICOM) her FETCH'te aynı COMMAREA alanlarının üzerine yazar ve çağırana yalnızca
/// son kayıt kalır; kayıt yoksa '00' döner ve veri gelmez. Yeni sistem bütün kayıtları liste olarak, kayıt yoksa boş liste
/// ile 200 döndürür (api-design.md §3 sapma tablosu, :139 ve :140). Testler yeni sistemin bu tanımlı davranışını ölçer.
/// Belgede tanımlı olmayan iki şey hiçbir testte doğrulanmaz: liste elemanlarının sırası (eski cursor'larda ORDER BY yok,
/// lgipdb01.cbl:89-118, :120-149) ve müşteri numarasıyla sorgudaki 21 satır sınırı (:906-909; 17-26 bu yüzden atlandı).
/// </para>
/// <para>
/// Ortak Doğrulanmayanlar: 'FINAL' işareti (lgipdb01.cbl:708, :813; COMMAREA'ya ait, API'de karşılığı yok),
/// <c>lastChanged</c>'in değeri ve biçimi (sunucu belirler, §5; eski sistemde COMMERCIAL.RequestDate, 17 Kod gözlemleri 4
/// ve 18), boş gönderilen <c>brokersReference</c> (boş metin beklentisi yazılmaz; TicariVerisi), tarihlerin hangi tablodan
/// geldiği (belgede tanımsız; TicariVerisi sınıf açıklaması).
/// </para>
/// </summary>
public sealed class TicariSorgulaTests : IClassFixture<GenAppApiFactory>
{
    private readonly HttpClient _istemci;

    public TicariSorgulaTests(GenAppApiFactory fabrika) => _istemci = fabrika.CreateClient();

    // Tahmin 17-04, lgipdb01.cbl:680 (01ICOM SELECT'i SQLCODE = 0; CA-RETURN-CODE :258'deki '00' olarak kalır, veri
    // :703-704'te COMMAREA'ya taşınır). Poliçe 10'a güvenilmez: örnek verinin test veritabanına yüklü olduğu garanti değil.
    // Bu yüzden poliçe 10'un değerleriyle (TicariVerisi.Police10) POST edilen poliçe sorgulanır. Tahmindeki B4 ön koşulu
    // (düzenek SELECT'in host değişkenlerini satırın değerleriyle doldurur) gerçek veritabanında değerlerin satırdan
    // gelmesine karşılık gelir.
    // Eski sistemle fark ölçülmez: eski sistem tarihleri ve zaman damgasını COMMERCIAL'dan (StartDate, RenewalDate,
    // RequestDate) okur, POLICY'den değil (:633-635, :652-654). Örnek veride bu değerler farklıdır, ama POST ile kurulan
    // poliçede LGAPDB01 iki tabloya aynı tarihleri yazar (16 Kod gözlemleri 7); iki sistem de gönderilen tarihleri döndürür.
    // Broker ve ödeme 0 gönderilir: eski sistem bunları okumaz ve her zaman 0 döndürür (:245, 17 Kod gözlemleri 5), yeni
    // sistem kayıttaki değeri döndürür (api-design.md:142); iki sistemde de 0'dır.
    // Beklenen: 200 + poliçe (api-design.md §2.5 :71, §3); policyNumber ve customerNumber adresteki numaralar; alanlar POST
    // edilen değerler: tarihler yyyy-MM-dd (§1 :17), metinler kırpılmış (§1 :18, ADR 0002 Karar 6); lastChanged boş
    // olmayan bir metin (§5).
    // Doğrulanmayanlar: brokersReference (boş gönderilir), lastChanged'in değeri ve biçimi, 'FINAL' işareti (:708),
    // tarihlerin kaynak tablosu.
    [Fact]
    [Trait("Operasyon", "ticari-sorgula")]
    public async Task LGIPDB01_satir_680_police_var_00_doner()
    {
        var musteriNumarasi = await _istemci.MusteriKurAsync(MusteriVerisi.AndrewPandy);
        var policeNumarasi = await _istemci.TicariPoliceKurAsync(musteriNumarasi, TicariVerisi.Police10);

        using var cevap = await _istemci.GetAsync(
            TicariYardimcilari.TicariAdresiNumarayla(musteriNumarasi, policeNumarasi));

        await cevap.DurumKoduDogrulaAsync(HttpStatusCode.OK);
        var police = await cevap.JsonOkuAsync();
        Assert.Equal(policeNumarasi, PoliceYardimcilari.PoliceNumarasiOku(police));
        Assert.Equal(musteriNumarasi, ApiYardimcilari.MusteriNumarasiOku(police));
        PoliceYardimcilari.LastChangedOku(police);
        PoliceYardimcilari.PoliceAlanlariDogrula(
            police, PoliceYardimcilari.AlanlarHaric(TicariVerisi.Police10, TicariVerisi.BosGonderilenAlanlar));
    }

    // Tahmin 17-05, lgipdb01.cbl:714 (01ICOM'un WHERE'i müşteri ve poliçe numarasını birlikte ister, :671-677; [Db2 bilgisi]
    // satır yok, SQLCODE = 100 (:712) → CA-RETURN-CODE '01'). Poliçe numarası tek başına yetmez.
    // Kurulum: A müşterisi ve poliçesi (tahminde müşteri 1 ve poliçe 10) ile B müşterisi (tahminde müşteri 5) POST ile
    // kurulur; A'nın poliçesi B'nin adresinden sorgulanır.
    // Beklenen: 404, type "not-found", returnCode "01" (api-design.md §3 :94). Eski sistemle aynı.
    // Doğrulanmayanlar: COMMAREA'nın CA-RETURN-CODE dışında değişmemesi ve 'FINAL'in yazılmaması (API'de karşılığı yok).
    [Fact]
    [Trait("Operasyon", "ticari-sorgula")]
    public async Task LGIPDB01_satir_714_musteri_police_eslesmiyor_01_doner()
    {
        var musteriA = await _istemci.MusteriKurAsync(MusteriVerisi.AndrewPandy);
        var policeNumarasi = await _istemci.TicariPoliceKurAsync(musteriA, TicariVerisi.Police10);
        var musteriB = await _istemci.MusteriKurAsync(MusteriVerisi.AndrewPandy);

        using var cevap = await _istemci.GetAsync(TicariYardimcilari.TicariAdresiNumarayla(musteriB, policeNumarasi));

        await cevap.HataGovdesiDogrulaAsync(HttpStatusCode.NotFound, "not-found", "01");
    }

    // Tahmin 17-07, lgipdb01.cbl:714 (poliçe yok; WHERE (:676-677) satır bulamaz, [Db2 bilgisi] SQLCODE = 100 → '01').
    // Beklenen: 404, type "not-found", returnCode "01" (api-design.md §3 :94). Eski sistemle aynı.
    // Kurulum: tahmindeki müşteri 1 gibi var olan bir müşteri POST ile kurulur. Poliçe kurulmaz; bu test ticari-ekle'ye
    // bağlı değildir.
    // Numara seçimi: Tahmin 0 kullanır. API'nin 0 gibi pozitif olmayan bir numarayı doğrulamayla 400 ile reddedip
    // reddetmeyeceği api-design.md'de tanımlı değil; bu test o kuralı değil "kayıt yok" kuralını ölçmeli. Bu yüzden geçerli
    // ama hiç atanmayan bir numara seçildi (PoliceYardimcilari.AtanmayanNumara):
    // - 999999999 dokuz hanedir, DB2-POLICYNUM-INT'e (PIC S9(9) COMP, lgipdb01.cbl:168) kesilmeden sığar. COBOL'da da
    //   aynı yol çalışır: satır yok, SQLCODE = 100.
    // - Kimlik değeri 1000001'den başlar (docs/database-schema.md) ve policyNumber istekte gönderilse de yok sayılır
    //   (§1); bu numaraya testlerde pratikte ulaşılmaz.
    // Doğrulanmayanlar: COMMAREA'nın CA-RETURN-CODE dışında değişmemesi ve 'FINAL'in yazılmaması (API'de karşılığı yok).
    [Fact]
    [Trait("Operasyon", "ticari-sorgula")]
    public async Task LGIPDB01_satir_714_police_yok_01_doner()
    {
        var musteriNumarasi = await _istemci.MusteriKurAsync(MusteriVerisi.AndrewPandy);

        using var cevap = await _istemci.GetAsync(
            TicariYardimcilari.TicariAdresiNumarayla(musteriNumarasi, PoliceYardimcilari.AtanmayanNumara));

        await cevap.HataGovdesiDogrulaAsync(HttpStatusCode.NotFound, "not-found", "01");
    }

    // Tahmin 17-08, lgipdb01.cbl:714 (müşteri yok; WHERE (:674-675) satır bulamaz, [Db2 bilgisi] SQLCODE = 100 → '01').
    // Sorgulamada 70 yolu yoktur: '70' lgipol01.cbl'de de lgipdb01.cbl'de de geçmez. api-design.md §3'te 70 "Poliçe
    // eklenirken müşteri bulunamadı" (customer-not-found, :96) anlamındadır; sorgulamada müşterinin olmaması 01 "Kayıt
    // bulunamadı" (not-found, :94) demektir.
    // Beklenen: 404, type "not-found", returnCode "01" (§3 :94); customer-not-found / 70 değil. Eski sistemle aynı.
    // Kurulum: tahmindeki poliçe 10 gibi var olan bir poliçe için müşteri ve poliçe POST ile kurulur; sorgu var olmayan
    // müşterinin adresinden yapılır. Tahmin müşteri 0 kullanır; PoliceYardimcilari.AtanmayanNumara (999999999) seçildi
    // (gerekçe 17-07'de; müşteri numarası da DB2-CUSTOMERNUM-INT'e, PIC S9(9) COMP, lgipdb01.cbl:167, sığar).
    // Doğrulanmayanlar: COMMAREA'nın CA-RETURN-CODE dışında değişmemesi (API'de karşılığı yok).
    [Fact]
    [Trait("Operasyon", "ticari-sorgula")]
    public async Task LGIPDB01_satir_714_musteri_yok_01_doner()
    {
        var musteriNumarasi = await _istemci.MusteriKurAsync(MusteriVerisi.AndrewPandy);
        var policeNumarasi = await _istemci.TicariPoliceKurAsync(musteriNumarasi, TicariVerisi.Police10);

        using var cevap = await _istemci.GetAsync(
            TicariYardimcilari.TicariAdresiNumarayla(PoliceYardimcilari.AtanmayanNumara, policeNumarasi));

        await cevap.HataGovdesiDogrulaAsync(HttpStatusCode.NotFound, "not-found", "01");
    }

    // Tahmin 17-16, lgipdb01.cbl:703 (DB2-POLICY-COMMON, :245'teki INITIALIZE değerleriyle CA-POLICY-COMMON'a taşınır:
    // ticari SELECT'in sütun listesinde BROKERID, BROKERSREF ve PAYMENT yoktur, :632-651). Eski sistem POLICY'deki değerler
    // ne olursa olsun CA-BROKERID = 0000000000, CA-BROKERSREF = boşluk, CA-PAYMENT = 000000 döndürür (17 Kod gözlemleri 5).
    // Yeni sistem bundan bilinçli olarak ayrılır: "Ticari sorgula (17a-17d): brokerId, brokersReference, payment" satırı,
    // kayıttaki değerlerin döneceğini söyler (api-design.md §3 sapma tablosu :142; ADR 0002 Karar 9). Test bu tanımlı yeni
    // davranışı ölçer; eşdeğerlik ölçümünde bilinçli sapma olarak raporlanır.
    // Girdi: poliçe 10, ama ortak alanlar sıfırdan farklı (TicariVerisi.Police10BrokerDegerleriyle; tahmindeki seçilmiş
    // değerler 1234, BRK0000001, 500).
    // Beklenen: 200 (§2.5 :71, §3); brokerId 1234, brokersReference "BRK0000001", payment 500 (:142).
    // Doğrulanmayanlar: diğer alanlar (17-04 ölçer), lastChanged, 'FINAL' işareti.
    [Fact]
    [Trait("Operasyon", "ticari-sorgula")]
    public async Task LGIPDB01_satir_703_broker_ve_odeme_sifirdan_farkli_00_doner()
    {
        var musteriNumarasi = await _istemci.MusteriKurAsync(MusteriVerisi.AndrewPandy);
        var girdi = TicariVerisi.Police10BrokerDegerleriyle;
        var policeNumarasi = await _istemci.TicariPoliceKurAsync(musteriNumarasi, girdi);

        using var cevap = await _istemci.GetAsync(
            TicariYardimcilari.TicariAdresiNumarayla(musteriNumarasi, policeNumarasi));

        await cevap.DurumKoduDogrulaAsync(HttpStatusCode.OK);
        PoliceYardimcilari.PoliceAlanlariDogrula(
            await cevap.JsonOkuAsync(),
            new Dictionary<string, object>
            {
                ["brokerId"] = girdi["brokerId"],
                ["brokersReference"] = girdi["brokersReference"],
                ["payment"] = girdi["payment"],
            });
    }

    // Tahmin 17-17, lgipdb01.cbl:784 (02ICOM SELECT'i SQLCODE = 0; WHERE yalnızca poliçe numarasına bakar, :778-781; satırın
    // müşteri numarası :797'de CA-Customer-Num'a geri yazılır). Eski sistemde girdideki müşteri (tahminde 1, poliçe 9'un
    // sahibi 5) yok sayılır; yeni sistemde adreste müşteri yoktur (api-design.md §2.5 :72).
    // Kurulum: müşteri ve poliçe 9'un değerleriyle (TicariVerisi.Police9) poliçe POST ile kurulur.
    // Beklenen: 200 (§2.5 :72, §3); customerNumber poliçenin sahibi (§4.2: customerNumber cevapta döner).
    // Doğrulanmayanlar: diğer alanlar (17-04 aynı alanları 17a için ölçer), lastChanged, 'FINAL' işareti (:813).
    [Fact]
    [Trait("Operasyon", "ticari-sorgula")]
    public async Task LGIPDB01_satir_784_yalnizca_police_numarasiyla_00_doner()
    {
        var musteriNumarasi = await _istemci.MusteriKurAsync(MusteriVerisi.AndrewPandy);
        var policeNumarasi = await _istemci.TicariPoliceKurAsync(musteriNumarasi, TicariVerisi.Police9);

        using var cevap = await _istemci.GetAsync(TicariYardimcilari.PoliceNumarasiylaAdres(policeNumarasi));

        await cevap.DurumKoduDogrulaAsync(HttpStatusCode.OK);
        Assert.Equal(musteriNumarasi, ApiYardimcilari.MusteriNumarasiOku(await cevap.JsonOkuAsync()));
    }

    // Tahmin 17-23, lgipdb01.cbl:891 (03ICOM Cust_Cursor'ın FETCH'i SQLCODE = 0; satır :901-904'te COMMAREA'ya taşınır,
    // ikinci FETCH 100 döner ve döngü :850'de biter; CA-RETURN-CODE :258'deki '00' olarak kalır). Eski sistemde COMMAREA'da
    // müşterinin tek ticari kaydı (poliçe 9) bulunur; yeni sistem bunu tek elemanlı liste olarak döndürür (api-design.md
    // §2.5 :73: "poliçe listesi"; §3 :139: bulunan kayıtların hepsi liste olarak döner).
    // Kurulum: müşteri ve poliçe 9'un değerleriyle (TicariVerisi.Police9) tek poliçe POST ile kurulur.
    // Beklenen: 200 + 1 elemanlı liste; elemanın policyNumber ve customerNumber'ı kurulan poliçeninki, alanları POST edilen
    // değerler (§4.2, §4.6; tarihler yyyy-MM-dd, metinler kırpılmış).
    // Doğrulanmayanlar: tahmindeki müşteri 5'in motor poliçesinin (poliçe 3, db2cre.jcl:969-973) listeye girmemesi: motor
    // poliçesi kurmak başka dilimin operasyonuna (motor-ekle) bağlı olduğu için kurulmaz. brokersReference (boş gönderilir),
    // lastChanged.
    [Fact]
    [Trait("Operasyon", "ticari-sorgula")]
    public async Task LGIPDB01_satir_891_musteri_numarasiyla_tek_police_00_doner()
    {
        var musteriNumarasi = await _istemci.MusteriKurAsync(MusteriVerisi.AndrewPandy);
        var policeNumarasi = await _istemci.TicariPoliceKurAsync(musteriNumarasi, TicariVerisi.Police9);

        using var cevap = await _istemci.GetAsync(TicariYardimcilari.TicariAdresi(musteriNumarasi));

        await cevap.DurumKoduDogrulaAsync(HttpStatusCode.OK);
        var police = Assert.Single(TicariYardimcilari.PoliceListesiOku(await cevap.JsonOkuAsync()));
        Assert.Equal(policeNumarasi, PoliceYardimcilari.PoliceNumarasiOku(police));
        Assert.Equal(musteriNumarasi, ApiYardimcilari.MusteriNumarasiOku(police));
        PoliceYardimcilari.PoliceAlanlariDogrula(
            police, PoliceYardimcilari.AlanlarHaric(TicariVerisi.Police9, TicariVerisi.BosGonderilenAlanlar));
    }

    // Tahmin 17-24, lgipdb01.cbl:850 (Cust_Cursor'ın ilk FETCH'i [Db2 bilgisi] SQLCODE = 100 döner; :891 yanlış olur, hiçbir
    // alan taşınmaz ve döngü UNTIL SQLCODE > 0 ile biter). Eski sistemde '01' yazılmaz, CA-RETURN-CODE :258'deki '00'
    // kalır ve veri gelmez.
    // Beklenen: 200 ve boş liste [] (api-design.md §3 sapma tablosu :140, "Ticari sorgula, müşteri ya da posta koduyla:
    // kayıt yok": korunan davranış, '00'ın karşılığı).
    // Kurulum: tahmindeki müşteri 6 gibi ticari kaydı olmayan bir müşteri POST ile kurulur. Poliçe kurulmaz; bu test
    // ticari-ekle'ye bağlı değildir.
    // Doğrulanmayanlar: tahmindeki müşteri 6'nın "türü C ama HOUSE satırı olan" poliçesi (poliçe 7, db2cre.jcl:897-901,
    // :1068): API'den kurulamaz.
    [Fact]
    [Trait("Operasyon", "ticari-sorgula")]
    public async Task LGIPDB01_satir_850_musteri_numarasiyla_kayit_yok_00_doner()
    {
        var musteriNumarasi = await _istemci.MusteriKurAsync(MusteriVerisi.AndrewPandy);

        using var cevap = await _istemci.GetAsync(TicariYardimcilari.TicariAdresi(musteriNumarasi));

        await cevap.DurumKoduDogrulaAsync(HttpStatusCode.OK);
        Assert.Empty(TicariYardimcilari.PoliceListesiOku(await cevap.JsonOkuAsync()));
    }

    // Tahmin 17-25, lgipdb01.cbl:901 (her başarılı FETCH, :901-904'te CA-CUSTOMER-NUM, CA-POLICY-NUM, CA-POLICY-COMMON ve
    // CA-COMMERCIAL'ın üzerine yazar). Eski sistemde iki satırlı sonuçta 3 FETCH yapılır ve çağırana yalnızca son satır
    // kalır (17 Kod gözlemleri 8). Yeni sistem bundan bilinçli olarak ayrılır: bulunan kayıtların hepsi liste olarak döner
    // (api-design.md §3 sapma tablosu :139; ADR 0002 Karar 9). Test bu tanımlı yeni davranışı ölçer; eşdeğerlik ölçümünde
    // bilinçli sapma olarak raporlanır.
    // Kurulum: tahmindeki satırlar (03ICOM bölümü): satır 1 poliçe 9 (TicariVerisi.Police9), satır 2 poliçe 9 ile aynı ama
    // customer "SATIR-02" (TicariVerisi.Satir2IsletmeAdi, seçilmiş değer). İki poliçe aynı müşteri için POST ile kurulur.
    // Beklenen: 200 + 2 elemanlı liste; poliçe numaraları kurulan iki poliçeninki, her elemanın customerNumber'ı müşteri,
    // customer'ı o poliçe için gönderilen değer. Elemanlar sırasız karşılaştırılır.
    // Doğrulanmayanlar: elemanların sırası (eski cursor'da ORDER BY yok, lgipdb01.cbl:89-118; belgede sıra tanımsız), 21
    // satır sınırı (:906-909; belgede yeni sistem için tanımsız, 17-26 atlandı), ikinci poliçenin numarasının değeri
    // (tahminde 1000001; yeni sistemde veritabanı verir), customer dışındaki alanlar (17-23 ölçer).
    [Fact]
    [Trait("Operasyon", "ticari-sorgula")]
    public async Task LGIPDB01_satir_901_musteri_numarasiyla_iki_police_00_doner()
    {
        var musteriNumarasi = await _istemci.MusteriKurAsync(MusteriVerisi.AndrewPandy);
        var satir1 = TicariVerisi.Police9;
        var satir2 = new Dictionary<string, object>(TicariVerisi.Police9) { ["customer"] = TicariVerisi.Satir2IsletmeAdi };
        var police1 = await _istemci.TicariPoliceKurAsync(musteriNumarasi, satir1);
        var police2 = await _istemci.TicariPoliceKurAsync(musteriNumarasi, satir2);

        using var cevap = await _istemci.GetAsync(TicariYardimcilari.TicariAdresi(musteriNumarasi));

        await cevap.DurumKoduDogrulaAsync(HttpStatusCode.OK);
        var liste = TicariYardimcilari.PoliceListesiOku(await cevap.JsonOkuAsync());
        IkiPoliceDogrula(liste, musteriNumarasi, (police1, satir1), (police2, satir2));
    }

    // Tahmin 17-33, lgipdb01.cbl:973 (05ICOM Zip_Cursor'ın FETCH'i SQLCODE = 0; tek koşulu Commercial.Zipcode =
    // :CA-B-POSTCODE'dur, :147-148; satırın müşteri numarası :983'te CA-CUSTOMER-NUM'a yazılır ve girdideki müşteri (tahminde
    // 5, poliçe 10'un sahibi 1) ezilir). Yeni sistemde girdide müşteri yoktur (api-design.md §2.5 :74); poliçenin sahibi
    // cevaptaki customerNumber'dır. Tek kayıt tek elemanlı liste olarak döner (§3 :139).
    // Kurulum: müşteri ve poliçe 10'un değerleriyle bir poliçe POST edilir; posta kodu poliçe 10'unki (SO212JN, :1189)
    // değil, teste özgü bir koddur (TicariVerisi.OzguPostaKodu, seçilmiş değer). Gerekçe: veritabanı sıfırlanmaz ve
    // SO212JN her çalıştırmada eklenen poliçelerde de bulunur; tahminin "posta kodu için tam 1 satır" koşulu ancak özgü bir
    // kodla kurulabilir.
    // Beklenen: 200 + 1 elemanlı liste; elemanın policyNumber'ı kurulan poliçeninki, customerNumber'ı poliçenin sahibi.
    // Doğrulanmayanlar: diğer alanlar (17-23 aynı alanları 17c için ölçer), lastChanged, posta kodunun büyük harfe çevrilip
    // çevrilmediği (değer zaten büyük harf).
    [Fact]
    [Trait("Operasyon", "ticari-sorgula")]
    public async Task LGIPDB01_satir_973_posta_koduyla_tek_police_00_doner()
    {
        var postaKodu = TicariVerisi.OzguPostaKodu();
        var musteriNumarasi = await _istemci.MusteriKurAsync(MusteriVerisi.AndrewPandy);
        var policeNumarasi = await _istemci.TicariPoliceKurAsync(
            musteriNumarasi, new Dictionary<string, object>(TicariVerisi.Police10) { ["postcode"] = postaKodu });

        using var cevap = await _istemci.GetAsync(TicariYardimcilari.PostaKoduylaAdres(postaKodu));

        await cevap.DurumKoduDogrulaAsync(HttpStatusCode.OK);
        var police = Assert.Single(TicariYardimcilari.PoliceListesiOku(await cevap.JsonOkuAsync()));
        Assert.Equal(policeNumarasi, PoliceYardimcilari.PoliceNumarasiOku(police));
        Assert.Equal(musteriNumarasi, ApiYardimcilari.MusteriNumarasiOku(police));
    }

    // Tahmin 17-34, lgipdb01.cbl:933 (Zip_Cursor'ın ilk FETCH'i [Db2 bilgisi] SQLCODE = 100 döner; :973 yanlış olur, hiçbir
    // alan taşınmaz ve döngü UNTIL SQLCODE > 0 ile biter). Eski sistemde CA-RETURN-CODE :258'deki '00' kalır ve veri gelmez.
    // Bu test 17-24'ün (müşteri numarasıyla, kayıt yok) posta kodu karşılığıdır.
    // Beklenen: 200 ve boş liste [] (api-design.md §3 sapma tablosu :140: korunan davranış).
    // Kurulum yok: hiçbir kayıtta olmayan, teste özgü bir posta kodu sorgulanır (TicariVerisi.OzguPostaKodu, seçilmiş değer;
    // gerekçe 17-33'te ve TicariVerisi'nde). Bu test hiçbir operasyonun etkin olmasına bağlı değildir.
    // Doğrulanmayanlar: tahminin ayırt edici koşulu, "posta kodu yalnızca HOUSE'ta var" (SO211UP, HOUSE 6, db2cre.jcl:1057):
    // konut poliçesi kurmak başka dilimin operasyonuna (konut-ekle) bağlı olduğu için kurulmaz; konut posta kodlarının ticari
    // sorguya girmemesi bu testte sınanmamaktadır.
    [Fact]
    [Trait("Operasyon", "ticari-sorgula")]
    public async Task LGIPDB01_satir_933_posta_koduyla_kayit_yok_00_doner()
    {
        using var cevap = await _istemci.GetAsync(TicariYardimcilari.PostaKoduylaAdres(TicariVerisi.OzguPostaKodu()));

        await cevap.DurumKoduDogrulaAsync(HttpStatusCode.OK);
        Assert.Empty(TicariYardimcilari.PoliceListesiOku(await cevap.JsonOkuAsync()));
    }

    // Tahmin 17-35, lgipdb01.cbl:983 (her başarılı FETCH, :983-986'da CA-CUSTOMER-NUM, CA-POLICY-NUM, CA-POLICY-COMMON ve
    // CA-COMMERCIAL'ın üzerine yazar). Eski sistemde iki satırlı sonuçta yalnızca son satır kalır (17 Kod gözlemleri 8).
    // Yeni sistem bundan bilinçli olarak ayrılır: bulunan kayıtların hepsi liste olarak döner (api-design.md §3 sapma
    // tablosu :139; ADR 0002 Karar 9). Test bu tanımlı yeni davranışı ölçer; eşdeğerlik ölçümünde bilinçli sapma olarak
    // raporlanır.
    // Kurulum: tahmindeki satırlar (05ICOM bölümü, 03ICOM'daki gibi): satır 1 poliçe 9, satır 2 poliçe 9 ile aynı ama
    // customer "SATIR-02" (seçilmiş değer). Tahmin ikisini poliçe 9'un posta koduyla (BB104BX, :1232) sorgular; burada ikisi
    // de aynı, teste özgü posta koduyla POST edilir (TicariVerisi.OzguPostaKodu, seçilmiş değer; gerekçe 17-33'te).
    // Beklenen: 200 + 2 elemanlı liste; poliçe numaraları kurulan iki poliçeninki, her elemanın customerNumber'ı müşteri,
    // customer'ı o poliçe için gönderilen değer. Elemanlar sırasız karşılaştırılır.
    // Doğrulanmayanlar: elemanların sırası (eski cursor'da ORDER BY yok, lgipdb01.cbl:120-149; belgede sıra tanımsız),
    // ikinci poliçenin numarasının değeri, customer dışındaki alanlar.
    [Fact]
    [Trait("Operasyon", "ticari-sorgula")]
    public async Task LGIPDB01_satir_983_posta_koduyla_iki_police_00_doner()
    {
        var postaKodu = TicariVerisi.OzguPostaKodu();
        var musteriNumarasi = await _istemci.MusteriKurAsync(MusteriVerisi.AndrewPandy);
        var satir1 = new Dictionary<string, object>(TicariVerisi.Police9) { ["postcode"] = postaKodu };
        var satir2 = new Dictionary<string, object>(satir1) { ["customer"] = TicariVerisi.Satir2IsletmeAdi };
        var police1 = await _istemci.TicariPoliceKurAsync(musteriNumarasi, satir1);
        var police2 = await _istemci.TicariPoliceKurAsync(musteriNumarasi, satir2);

        using var cevap = await _istemci.GetAsync(TicariYardimcilari.PostaKoduylaAdres(postaKodu));

        await cevap.DurumKoduDogrulaAsync(HttpStatusCode.OK);
        var liste = TicariYardimcilari.PoliceListesiOku(await cevap.JsonOkuAsync());
        IkiPoliceDogrula(liste, musteriNumarasi, (police1, satir1), (police2, satir2));
    }

    /// <summary>
    /// İki elemanlı liste cevabını sırasız doğrular (17-25, 17-35): poliçe numaraları kurulan iki poliçeninkidir; her
    /// elemanın <c>customerNumber</c>'ı müşteri, <c>customer</c>'ı o poliçe için gönderilen değerdir.
    /// </summary>
    private static void IkiPoliceDogrula(
        IReadOnlyList<JsonElement> liste,
        int musteriNumarasi,
        (int Numara, IReadOnlyDictionary<string, object> Girdi) birinci,
        (int Numara, IReadOnlyDictionary<string, object> Girdi) ikinci)
    {
        Assert.Equal(2, liste.Count);
        Assert.Equal(
            new[] { birinci.Numara, ikinci.Numara }.Order(),
            liste.Select(PoliceYardimcilari.PoliceNumarasiOku).Order());

        foreach (var (numara, girdi) in new[] { birinci, ikinci })
        {
            var police = liste.Single(eleman => PoliceYardimcilari.PoliceNumarasiOku(eleman) == numara);
            Assert.Equal(musteriNumarasi, ApiYardimcilari.MusteriNumarasiOku(police));
            PoliceYardimcilari.PoliceAlanlariDogrula(
                police, new Dictionary<string, object> { ["customer"] = girdi["customer"] });
        }
    }
}
