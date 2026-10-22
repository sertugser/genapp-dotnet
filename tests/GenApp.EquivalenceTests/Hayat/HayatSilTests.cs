using System.Net;
using GenApp.EquivalenceTests.Altyapi;

namespace GenApp.EquivalenceTests.Hayat;

/// <summary>
/// Hayat sigortası poliçesi sil: <c>DELETE /api/customers/{customerNumber}/endowment-policies/{policyNumber}</c> (eski
/// program LGDPOL01 → LGDPDB01 → LGDPVS01). Tahminler: tests/predictions/15-endowment-delete.md. Beklenen cevaplar:
/// docs/api-design.md §2.4, §3.
/// Her test kendi müşterisini POST ile kurar (musteri-ekle). 15-01 ve 15-03 poliçeyi POST ile kurar; bu testler hayat-ekle
/// etkin değilken kırmızı olur. 15-03 ayrıca silinmemesi gereken poliçeyi GET ile okur ve hayat-sorgula'ya bağlıdır. 15-02
/// yalnızca müşteri POST eder. Etkinleştirme sırası: hayat-ekle, hayat-sorgula, hayat-guncelle, hayat-sil.
/// Silme veriyi yok eder: tahmin dosyaları silme testlerinin en sonda ya da taze yüklemeyle çalışmasını önerir. Bu testler
/// yalnızca kendi kurdukları poliçeleri siler; örnek veriye (poliçe 4, 5) dokunmaz.
/// 15-01'in adındaki "hayat", aynı program ve satıra dayanan motor ve konut silme testlerinden ayırt etmek içindir.
/// </summary>
public sealed class HayatSilTests : IClassFixture<GenAppApiFactory>
{
    private readonly HttpClient _istemci;

    public HayatSilTests(GenAppApiFactory fabrika) => _istemci = fabrika.CreateClient();

    // Tahmin 15-01, lgdpdb01.cbl:138 (CA-RETURN-CODE '00' baştan yazılır; DELETE (:189-194) SQLCODE = 0 döndüğü için :198
    // yanlış olur ve değer değişmez). Tahmin poliçe 4'ü siler; poliçe 4'ün değerleriyle POST edilen poliçe silinir.
    // Ön koşul [B4] gerçek veride aynı kod yoluna karşılık gelir (15 "Ön koşullu satırlar").
    // Beklenen: 204 (api-design.md §2.4, §3).
    // Kaydın silindiği GET ile görülebilirdi, ama bu, hayat-sil testini kalıcılık için hayat-sorgula'ya bağlardı; burada
    // GET yapılmaz.
    // Doğrulanmayanlar: ENDOWMENT satırının cascade ile silinmesi ([Db2 bilgisi]; yabancı anahtarlar ON DELETE CASCADE,
    // docs/database-schema.md; db2cre.jcl:214-216), VSAM KSDSPOLY kaydının silinmesi (yeni sistemde VSAM yok), kaydın
    // gerçekten silindiği.
    [Fact]
    [Trait("Operasyon", "hayat-sil")]
    public async Task LGDPDB01_satir_138_hayat_police_silinir_00_doner()
    {
        var musteriNumarasi = await _istemci.MusteriKurAsync(MusteriVerisi.AndrewPandy);
        var policeNumarasi = await _istemci.HayatPoliceKurAsync(musteriNumarasi, HayatVerisi.Police4);

        using var cevap = await _istemci.DeleteAsync(
            HayatYardimcilari.HayatAdresiNumarayla(musteriNumarasi, policeNumarasi));

        await cevap.DurumKoduDogrulaAsync(HttpStatusCode.NoContent);
    }

    // Tahmin 15-02, lgdpdb01.cbl:199 ([Db2 bilgisi] DELETE satır silmez ve SQLCODE = 100 döner; :198 IF SQLCODE NOT EQUAL 0
    // 100'ü de hata sayar → '90'). Kodun hemen üstündeki yorum (:196-197) "SQLCODE 100 başarılı sayılsın" der, ama kod
    // bunu yapmaz (15 Kod gözlemleri 5).
    // Beklenen: 500, type "database-error", returnCode "90": eşleme api-design.md §3; "Poliçe sil: poliçe yok" korunan
    // davranıştır (ADR 0002 Karar 9: yalnızca yorumda başka niyet belirten, tutarlı çalışan kodda eski davranış korunur).
    // Kurulum: tahmindeki müşteri 8 gibi var olan bir müşteri POST ile kurulur. Poliçe kurulmaz; bu test hayat-ekle'ye bağlı
    // değildir.
    // Numara seçimi: Tahmin poliçe 999 kullanır (var olmayan bir numara). PoliceYardimcilari.AtanmayanNumara
    // (999999999): dokuz hanelik numara DB2-POLICYNUM-INT'e (PIC S9(9) COMP, lgdpdb01.cbl:83) kesilmeden sığar ve COBOL'da
    // da aynı yol çalışır; kimlik değeri 1000001'den başlar ve policyNumber istekte gönderilse de yok sayılır (§1), bu
    // numaraya testlerde pratikte ulaşılmaz.
    // Doğrulanmayanlar: VSAM'a gidilmemesi, LGSTSQ hata mesajları (API'den görülmez).
    [Fact]
    [Trait("Operasyon", "hayat-sil")]
    public async Task LGDPDB01_satir_199_hayat_police_yok_90_doner()
    {
        var musteriNumarasi = await _istemci.MusteriKurAsync(MusteriVerisi.AndrewPandy);

        using var cevap = await _istemci.DeleteAsync(
            HayatYardimcilari.HayatAdresiNumarayla(musteriNumarasi, PoliceYardimcilari.AtanmayanNumara));

        await cevap.HataGovdesiDogrulaAsync(HttpStatusCode.InternalServerError, "database-error", "90");
    }

    // Tahmin 15-03, lgdpdb01.cbl:199 (DELETE'in WHERE'i müşteri ve poliçe numarasını birlikte ister, :192-193; [Db2 bilgisi]
    // satır silinmez, SQLCODE = 100 → '90'). Poliçe numarası tek başına yetmez; A'nın poliçesine dokunulmaz.
    // Kurulum: A müşterisi ve poliçesi (tahminde müşteri 8 ve poliçe 4) ile B müşterisi (tahminde müşteri 3) POST ile
    // kurulur; A'nın poliçesi B'nin adresinden silinmeye çalışılır.
    // Beklenen: 500, type "database-error", returnCode "90" (api-design.md §3 "Poliçe sil: poliçe yok", korunan davranış:
    // B'nin adresinde bu poliçe yoktur). Ardından A'nın poliçesi yerindedir: GET 200 (hayat-sorgula).
    // Doğrulanmayanlar: A'nın poliçesinin alanları (yalnızca varlığı ölçülür), VSAM'a gidilmemesi, LGSTSQ hata mesajları.
    [Fact]
    [Trait("Operasyon", "hayat-sil")]
    public async Task LGDPDB01_satir_199_hayat_musteri_police_eslesmiyor_90_doner()
    {
        var musteriA = await _istemci.MusteriKurAsync(MusteriVerisi.AndrewPandy);
        var policeNumarasi = await _istemci.HayatPoliceKurAsync(musteriA, HayatVerisi.Police4);
        var musteriB = await _istemci.MusteriKurAsync(MusteriVerisi.AndrewPandy);

        using var cevap = await _istemci.DeleteAsync(HayatYardimcilari.HayatAdresiNumarayla(musteriB, policeNumarasi));

        await cevap.HataGovdesiDogrulaAsync(HttpStatusCode.InternalServerError, "database-error", "90");
        await _istemci.HayatPoliceOkuAsync(musteriA, policeNumarasi, "Silinmemesi gereken poliçenin okunması");
    }
}
