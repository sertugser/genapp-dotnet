using System.Net;
using GenApp.EquivalenceTests.Altyapi;

namespace GenApp.EquivalenceTests.Konut;

/// <summary>
/// Konut poliçesi sil: <c>DELETE /api/customers/{customerNumber}/house-policies/{policyNumber}</c> (eski program
/// LGDPOL01 → LGDPDB01 → LGDPVS01). Tahminler: tests/predictions/11-house-delete.md. Beklenen cevaplar:
/// docs/api-design.md §2.3, §3.
/// Her test kendi müşterisini POST ile kurar (musteri-ekle). 11-01, 11-05 ve 11-07 silinecek poliçeyi POST ile kurar; bu
/// testler konut-ekle etkin değilken kırmızı olur. 11-05 ayrıca silinmemesi gereken poliçeyi GET ile okur ve
/// konut-sorgula'ya bağlıdır. 11-07 silmenin kalıcı olduğunu GET ile değil ikinci DELETE ile ölçer. 11-04 yalnızca
/// müşteri POST eder. Etkinleştirme sırası: konut-ekle, konut-sorgula, konut-guncelle, konut-sil.
/// Silme veriyi yok eder: tahmin dosyaları ("Test sırası ve veri bağımlılıkları", 08-11) silme testlerinin en sonda ya
/// da taze yüklemeyle çalışmasını önerir. Bu testler yalnızca kendi kurdukları poliçeleri siler; örnek veriye (poliçe 6,
/// 7, 8) dokunmaz.
/// 11-01'in adındaki "konut", aynı program ve satıra dayanan motor silme testinden ayırt etmek içindir.
/// </summary>
public sealed class KonutSilTests : IClassFixture<GenAppApiFactory>
{
    private readonly HttpClient _istemci;

    public KonutSilTests(GenAppApiFactory fabrika) => _istemci = fabrika.CreateClient();

    // Tahmin 11-01, lgdpdb01.cbl:138 (CA-RETURN-CODE '00' baştan yazılır; DELETE (:189-194) SQLCODE = 0 döndüğü için :198
    // yanlış olur ve değer değişmez). Tahmin poliçe 6'yı siler; poliçe 6'nın değerleriyle POST edilen poliçe silinir.
    // Ön koşul [B4] gerçek veride aynı kod yoluna karşılık gelir (11 "Ön koşullu satırlar").
    // Beklenen: 204 (api-design.md §2.3, §3).
    // Kaydın silindiği GET ile görülebilirdi, ama bu, konut-sil testini kalıcılık için konut-sorgula'ya bağlardı; burada
    // GET yapılmaz.
    // Doğrulanmayanlar: HOUSE satırının cascade ile silinmesi ([Db2 bilgisi]; yabancı anahtarlar ON DELETE CASCADE,
    // docs/database-schema.md), VSAM KSDSPOLY kaydının silinmesi (yeni sistemde VSAM yok), kaydın gerçekten silindiği
    // (11-07 ölçer).
    [Fact]
    [Trait("Operasyon", "konut-sil")]
    public async Task LGDPDB01_satir_138_konut_police_silinir_00_doner()
    {
        var musteriNumarasi = await _istemci.MusteriKurAsync(MusteriVerisi.AndrewPandy);
        var policeNumarasi = await _istemci.KonutPoliceKurAsync(musteriNumarasi, KonutVerisi.Police6);

        using var cevap = await _istemci.DeleteAsync(
            KonutYardimcilari.KonutAdresiNumarayla(musteriNumarasi, policeNumarasi));

        await cevap.DurumKoduDogrulaAsync(HttpStatusCode.NoContent);
    }

    // Tahmin 11-04, lgdpdb01.cbl:199 ([Db2 bilgisi] DELETE satır silmez ve SQLCODE = 100 döner; :198 IF SQLCODE NOT EQUAL 0
    // 100'ü de hata sayar → '90'). Kodun hemen üstündeki yorum (:196-197) "SQLCODE 100 başarılı sayılsın" der, ama kod
    // bunu yapmaz (11 Kod gözlemleri 6).
    // Beklenen: 500, type "database-error", returnCode "90": eşleme api-design.md §3 (:102); "Poliçe sil: poliçe yok"
    // korunan davranıştır (:133; ADR 0002 Karar 9: yalnızca yorumda başka niyet belirten, tutarlı çalışan kodda eski
    // davranış korunur). Ekipte "API eşlemesi 404 olabilir" diye not edilmişti; belge bununla çelişir ve eski sistemin
    // 90'ını 500 olarak korur.
    // Kurulum: tahmindeki müşteri 4 gibi var olan bir müşteri POST ile kurulur. Poliçe kurulmaz; bu test konut-ekle'ye
    // bağlı değildir.
    // Numara seçimi: Tahmin poliçe 0 kullanır. PoliceYardimcilari.AtanmayanNumara (999999999): API'nin 0'ı doğrulamayla
    // 400 ile reddedip reddetmeyeceği tanımlı değil; dokuz hanelik numara DB2-POLICYNUM-INT'e (PIC S9(9) COMP,
    // lgdpdb01.cbl:83) kesilmeden sığar ve COBOL'da da aynı yol çalışır; kimlik değeri 1000001'den başlar ve policyNumber
    // istekte gönderilse de yok sayılır (§1), bu numaraya testlerde pratikte ulaşılmaz.
    // Doğrulanmayanlar: VSAM'a gidilmemesi, LGSTSQ hata mesajları (API'den görülmez).
    [Fact]
    [Trait("Operasyon", "konut-sil")]
    public async Task LGDPDB01_satir_199_police_yok_90_doner()
    {
        var musteriNumarasi = await _istemci.MusteriKurAsync(MusteriVerisi.AndrewPandy);

        using var cevap = await _istemci.DeleteAsync(
            KonutYardimcilari.KonutAdresiNumarayla(musteriNumarasi, PoliceYardimcilari.AtanmayanNumara));

        await cevap.HataGovdesiDogrulaAsync(HttpStatusCode.InternalServerError, "database-error", "90");
    }

    // Tahmin 11-05, lgdpdb01.cbl:199 (DELETE'in WHERE'i müşteri ve poliçe numarasını birlikte ister, :192-193; [Db2 bilgisi]
    // satır silinmez, SQLCODE = 100 → '90'). Poliçe numarası tek başına yetmez; A'nın poliçesine dokunulmaz.
    // Kurulum: A müşterisi ve poliçesi (tahminde müşteri 4 ve poliçe 6) ile B müşterisi (tahminde müşteri 9) POST ile
    // kurulur; A'nın poliçesi B'nin adresinden silinmeye çalışılır.
    // Beklenen: 500, type "database-error", returnCode "90" (api-design.md §3 :102; :133 "Poliçe sil: poliçe yok", korunan
    // davranış: B'nin adresinde bu poliçe yoktur). Ardından A'nın poliçesi yerindedir: GET 200 (konut-sorgula).
    // Doğrulanmayanlar: A'nın poliçesinin alanları (yalnızca varlığı ölçülür), VSAM'a gidilmemesi, LGSTSQ hata mesajları.
    [Fact]
    [Trait("Operasyon", "konut-sil")]
    public async Task LGDPDB01_satir_199_musteri_police_eslesmiyor_90_doner()
    {
        var musteriA = await _istemci.MusteriKurAsync(MusteriVerisi.AndrewPandy);
        var policeNumarasi = await _istemci.KonutPoliceKurAsync(musteriA, KonutVerisi.Police6);
        var musteriB = await _istemci.MusteriKurAsync(MusteriVerisi.AndrewPandy);

        using var cevap = await _istemci.DeleteAsync(KonutYardimcilari.KonutAdresiNumarayla(musteriB, policeNumarasi));

        await cevap.HataGovdesiDogrulaAsync(HttpStatusCode.InternalServerError, "database-error", "90");
        await _istemci.KonutPoliceOkuAsync(musteriA, policeNumarasi, "Silinmemesi gereken poliçenin okunması");
    }

    // Tahmin 11-07, lgdpdb01.cbl:199 (sıra 11-01: poliçe silinmiştir; [Db2 bilgisi] ikinci DELETE satır silmez, SQLCODE =
    // 100 → '90').
    // Kurulum: müşteri ve poliçe POST ile kurulur; ilk DELETE (11-01'in isteği) 204 döner. Sıra 11-01 → 11-07 testin
    // içinde kurulur.
    // Beklenen: 500, type "database-error", returnCode "90" (api-design.md §3 :102; :133 "Poliçe sil: poliçe yok",
    // korunan davranış). Bu sonuç ilk silmenin kalıcı olduğunu da gösterir.
    // Doğrulanmayanlar: VSAM'a gidilmemesi, LGSTSQ hata mesajları.
    [Fact]
    [Trait("Operasyon", "konut-sil")]
    public async Task LGDPDB01_satir_199_silinmis_police_90_doner()
    {
        var musteriNumarasi = await _istemci.MusteriKurAsync(MusteriVerisi.AndrewPandy);
        var policeNumarasi = await _istemci.KonutPoliceKurAsync(musteriNumarasi, KonutVerisi.Police6);
        var adres = KonutYardimcilari.KonutAdresiNumarayla(musteriNumarasi, policeNumarasi);
        using (var ilkSilme = await _istemci.DeleteAsync(adres))
        {
            await ilkSilme.DurumKoduDogrulaAsync(HttpStatusCode.NoContent, "Ön koşul (ilk silme, 11-01)");
        }

        using var cevap = await _istemci.DeleteAsync(adres);

        await cevap.HataGovdesiDogrulaAsync(HttpStatusCode.InternalServerError, "database-error", "90");
    }
}
