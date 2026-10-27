using System.Net;
using GenApp.EquivalenceTests.Altyapi;

namespace GenApp.EquivalenceTests.Ticari;

/// <summary>
/// Ticari poliçe sil: <c>DELETE /api/customers/{customerNumber}/commercial-policies/{policyNumber}</c> (eski program
/// LGDPOL01 → LGDPDB01 → LGDPVS01). Tahminler: tests/predictions/18-commercial-delete.md. Beklenen cevaplar:
/// docs/api-design.md §2.5, §3.
/// Her test kendi müşterisini POST ile kurar (musteri-ekle). 18-11 yalnızca müşteri ister. 18-08 silinecek poliçeyi POST ile
/// kurar; ticari-ekle etkin değilken kırmızı olur. 18-10 ayrıca silinmemesi gereken poliçeyi GET ile okur ve hem ticari-ekle'ye
/// hem ticari-sorgula'ya bağlıdır. Etkinleştirme sırası: musteri-ekle, ticari-ekle, ticari-sorgula, ticari-sil.
/// Silme veriyi yok eder: tahmin dosyası ("Test sırası ve veri bağımlılıkları", 18) silme testlerinin en sonda ya da taze
/// yüklemeyle çalışmasını önerir. Bu testler yalnızca kendi kurdukları poliçeleri siler; örnek veriye (poliçe 9, 10) dokunmaz.
/// Test adlarının durum kısmındaki "ticari", aynı program ve satıra dayanan motor (18-08) ve konut (18-10, 18-11) silme
/// testlerinden ayırt etmek içindir.
/// </summary>
public sealed class TicariSilTests : IClassFixture<GenAppApiFactory>
{
    private readonly HttpClient _istemci;

    public TicariSilTests(GenAppApiFactory fabrika) => _istemci = fabrika.CreateClient();

    // Tahmin 18-08, lgdpdb01.cbl:138 (CA-RETURN-CODE '00' baştan yazılır; DELETE (:189-194) SQLCODE = 0 döndüğü için :198
    // yanlış olur ve değer değişmez). Tahmin poliçe 10'u siler; poliçe 10'un değerleriyle (TicariVerisi.Police10) POST edilen
    // poliçe silinir. Ön koşul [B4] (DELETE 0, DELETE FILE NORMAL) gerçek veride aynı kod yoluna karşılık gelir.
    // Beklenen: 204 (api-design.md §2.5 :75, §3).
    // Kaydın silindiği GET ile görülebilirdi, ama bu, ticari-sil testini kalıcılık için ticari-sorgula'ya bağlardı; burada
    // GET yapılmaz.
    // Doğrulanmayanlar: COMMERCIAL satırının cascade ile silinmesi ([Db2 bilgisi]; yabancı anahtar ON DELETE CASCADE,
    // db2cre.jcl:335-336, docs/database-schema.md), VSAM KSDSPOLY kaydının silinmesi (yeni sistemde VSAM yok), kaydın
    // gerçekten silindiği (bu sınıftaki hiçbir test ölçmez: 18-10'daki GET başka bir poliçenin yerinde kaldığını ölçer,
    // 18-11 hiç kurulmamış bir numarayı siler).
    [Fact]
    [Trait("Operasyon", "ticari-sil")]
    public async Task LGDPDB01_satir_138_ticari_police_silinir_00_doner()
    {
        var musteriNumarasi = await _istemci.MusteriKurAsync(MusteriVerisi.AndrewPandy);
        var policeNumarasi = await _istemci.TicariPoliceKurAsync(musteriNumarasi, TicariVerisi.Police10);

        using var cevap = await _istemci.DeleteAsync(
            TicariYardimcilari.TicariAdresiNumarayla(musteriNumarasi, policeNumarasi));

        await cevap.DurumKoduDogrulaAsync(HttpStatusCode.NoContent);
    }

    // Tahmin 18-10, lgdpdb01.cbl:199 (DELETE'in WHERE'i müşteri ve poliçe numarasını birlikte ister, :192-193; [Db2 bilgisi]
    // satır silinmez, SQLCODE = 100; :198 IF SQLCODE NOT EQUAL 0 100'ü de hata sayar → '90'). Poliçe numarası tek başına
    // yetmez; A'nın poliçesine dokunulmaz. Kodun hemen üstündeki yorum (:196-197) 100'ün başarı sayılacağını söyler, kod bunu
    // yapmaz (18 Kod gözlemleri 6).
    // Kurulum: A müşterisi ve poliçesi (tahminde müşteri 1 ve poliçe 10) ile B müşterisi (tahminde müşteri 5) POST ile
    // kurulur; A'nın poliçesi B'nin adresinden silinmeye çalışılır.
    // Beklenen: 500, type "database-error", returnCode "90": eşleme api-design.md §3 (:102); "Poliçe sil: poliçe yok"
    // korunan davranıştır (§3 sapma tablosu :135; ADR 0002 Karar 9: yalnızca yorumda başka niyet belirten, tutarlı çalışan
    // kodda eski davranış korunur). B'nin adresinde bu poliçe yoktur; eski sistem de aynı sonucu ('90') verir. Ardından A'nın
    // poliçesi yerindedir: GET 200 (ticari-sorgula).
    // Doğrulanmayanlar: A'nın poliçesinin alanları (yalnızca varlığı ölçülür), VSAM'a gidilmemesi (LGDPVS01 çağrılmaz,
    // :167-171 yalnızca DELETE başarılıysa), LGSTSQ hata mesajları.
    [Fact]
    [Trait("Operasyon", "ticari-sil")]
    public async Task LGDPDB01_satir_199_ticari_musteri_police_eslesmiyor_90_doner()
    {
        var musteriA = await _istemci.MusteriKurAsync(MusteriVerisi.AndrewPandy);
        var policeNumarasi = await _istemci.TicariPoliceKurAsync(musteriA, TicariVerisi.Police10);
        var musteriB = await _istemci.MusteriKurAsync(MusteriVerisi.AndrewPandy);

        using var cevap = await _istemci.DeleteAsync(TicariYardimcilari.TicariAdresiNumarayla(musteriB, policeNumarasi));

        await cevap.HataGovdesiDogrulaAsync(HttpStatusCode.InternalServerError, "database-error", "90");
        await _istemci.TicariPoliceOkuAsync(musteriA, policeNumarasi, "Silinmemesi gereken poliçenin okunması");
    }

    // Tahmin 18-11, lgdpdb01.cbl:199 ([Db2 bilgisi] DELETE satır silmez ve SQLCODE = 100 döner; :198 IF SQLCODE NOT EQUAL 0
    // 100'ü de hata sayar → '90'). Yorum-kod çelişkisi: kodun hemen üstündeki yorum (:196-197) "SQLCODE 0 ve 100 başarılı
    // sayılsın" der, ama kod :198-199'da 100 için de '90' yazar (18 Kod gözlemleri 6).
    // Beklenen: 500, type "database-error", returnCode "90": eşleme api-design.md §3 (:102); "Poliçe sil: poliçe yok"
    // korunan davranıştır (§3 sapma tablosu :135; ADR 0002 Karar 9). Eski sistemle aynı.
    // Kurulum: tahmindeki müşteri 1 gibi var olan bir müşteri POST ile kurulur. Poliçe kurulmaz; bu test ticari-ekle'ye
    // bağlı değildir.
    // Numara seçimi: Tahmin poliçe 0 kullanır. PoliceYardimcilari.AtanmayanNumara (999999999): API'nin 0'ı doğrulamayla
    // 400 ile reddedip reddetmeyeceği tanımlı değil; dokuz hanelik numara DB2-POLICYNUM-INT'e (PIC S9(9) COMP,
    // lgdpdb01.cbl:83, değer :150'de taşınır) kesilmeden sığar ve COBOL'da da aynı yol çalışır; kimlik değeri 1000001'den
    // başlar ve policyNumber istekte gönderilse de yok sayılır (§1), bu numaraya testlerde pratikte ulaşılmaz.
    // Doğrulanmayanlar: VSAM'a gidilmemesi, LGSTSQ hata mesajları (API'den görülmez).
    [Fact]
    [Trait("Operasyon", "ticari-sil")]
    public async Task LGDPDB01_satir_199_ticari_police_yok_90_doner()
    {
        var musteriNumarasi = await _istemci.MusteriKurAsync(MusteriVerisi.AndrewPandy);

        using var cevap = await _istemci.DeleteAsync(
            TicariYardimcilari.TicariAdresiNumarayla(musteriNumarasi, PoliceYardimcilari.AtanmayanNumara));

        await cevap.HataGovdesiDogrulaAsync(HttpStatusCode.InternalServerError, "database-error", "90");
    }
}
