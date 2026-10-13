using System.Net;
using GenApp.EquivalenceTests.Altyapi;

namespace GenApp.EquivalenceTests.Motor;

/// <summary>
/// Motor poliçesi sil: <c>DELETE /api/customers/{customerNumber}/motor-policies/{policyNumber}</c>
/// (eski program LGDPOL01 → LGDPDB01 → LGDPVS01, istek kodu <c>01DMOT</c>).
/// Tahminler: tests/predictions/07-motor-delete.md. Beklenen cevaplar: docs/api-design.md §2.2, §3.
/// Kurulum için müşteri ve poliçe <c>POST</c> ile kurulur, silmenin etkisi <c>GET</c> ile doğrulanır; bu test
/// musteri-ekle, musteri-sorgula, motor-ekle ve motor-sorgula etkin değilken kırmızı olur. Etkinleştirme sırası:
/// musteri-ekle, musteri-sorgula, motor-ekle, motor-sorgula, motor-guncelle, motor-sil.
/// Hangi tahmin satırlarının çevrilmediği ve nedeni: Motor/README.md.
/// </summary>
public sealed class MotorSilTests : IClassFixture<GenAppApiFactory>
{
    private readonly HttpClient _istemci;

    public MotorSilTests(GenAppApiFactory fabrika) => _istemci = fabrika.CreateClient();

    // Tahmin 07-01, lgdpdb01.cbl:138 (CA-RETURN-CODE '00'; DELETE SQLCODE = 0 ise değişmez, :198-202; VSAM silme
    // başarılıysa değişmez, lgdpvs01.cbl:86-91). DELETE yalnızca POLICY'den siler; MOTOR satırı yabancı anahtar
    // (ON DELETE CASCADE) nedeniyle birlikte gider (lgdpdb01.cbl:183-185; db2cre.jcl:287-288). Ön koşul: tahminde örnek
    // verideki poliçe 1 ve VSAM kaydı; burada müşteri ve poliçe, poliçe 1 ve motor 1'in değerleriyle (MotorVerisi.Ford)
    // POST ile kurulur (örnek verinin yüklü olduğu belgelerde kesin değil).
    // Beklenen: 204 (api-design.md §2.2, §3). Silmenin etkisi API'den iki yerde görünür: poliçe artık okunamaz
    // (GET 404, type "not-found", returnCode "01"; MOTOR satırının da gittiği buradan görülür), müşteri yerindedir
    // (tahmin: müşteri satırı değişmez; GET 200).
    // Doğrulanmayanlar: VSAM KSDSPOLY kaydının silinmesi (yeni sistemde VSAM yok), MOTOR satırının ayrıca silindiği
    // (yalnızca poliçenin okunamaması görünür), başka poliçelerin etkilenmemesi.
    [Fact]
    [Trait("Operasyon", "motor-sil")]
    public async Task LGDPDB01_satir_138_police_silinir_00_doner()
    {
        var musteriNumarasi = await _istemci.MusteriKurAsync(MusteriVerisi.AndrewPandy);
        var policeNumarasi = await _istemci.MotorPoliceKurAsync(musteriNumarasi, MotorVerisi.Ford);
        var adres = MotorYardimcilari.MotorAdresiNumarayla(musteriNumarasi, policeNumarasi);

        using var cevap = await _istemci.DeleteAsync(adres);

        await cevap.DurumKoduDogrulaAsync(HttpStatusCode.NoContent);

        using var okuma = await _istemci.GetAsync(adres);
        await okuma.HataGovdesiDogrulaAsync(HttpStatusCode.NotFound, "not-found", "01");

        using var musteri = await _istemci.GetAsync(ApiYardimcilari.MusteriAdresiNumarayla(musteriNumarasi));
        await musteri.DurumKoduDogrulaAsync(HttpStatusCode.OK, "Silme sonrası müşteri okuma");
    }
}
