using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GenApp.EquivalenceTests.Altyapi;

namespace GenApp.EquivalenceTests.Yoklama;

/// <summary>
/// LLM deneyi, EK YOKLAMA (ölçüm dışı; experiments/llm/README.md bölüm 6'daki dondurulmuş test setine dahil değildir).
/// Dondurulmuş testlerin ölçmediği, ama docs/api-design.md'de yazılı davranışları sınar. Yalnızca deney çalışma
/// kopyasına eklenir; tests/ altında değildir. Etiket: [Trait("Yoklama", "&lt;operasyon&gt;")]. Beklenen değerler
/// api-design.md'den gelir (bölüm numarası her yoklamada yazılı), LLM kodundan değil.
/// </summary>
public sealed class YoklamaTests : IClassFixture<GenAppApiFactory>
{
    private readonly HttpClient _istemci;

    public YoklamaTests(GenAppApiFactory fabrika) => _istemci = fabrika.CreateClient();

    private static Dictionary<string, string> Musteri() => MusteriVerisi.AndrewPandy.ToDictionary(a => a.Key, a => a.Value);

    private static Dictionary<string, object> Motor() => MotorVerisi.Ford.ToDictionary(a => a.Key, a => a.Value);

    private static async Task<string> Tip(HttpResponseMessage cevap)
    {
        var govde = await cevap.JsonOkuAsync();
        return govde.ValueKind == JsonValueKind.Object && govde.TryGetProperty("type", out var t) ? t.GetString() ?? "" : "";
    }

    // §1 "Posta kodu": müşteri ekle büyük harfe çevirerek kaydeder.
    [Fact]
    [Trait("Yoklama", "musteri-ekle")]
    public async Task Y01_posta_kodu_buyuk_harfe_cevrilir()
    {
        var govde = Musteri();
        govde["postcode"] = "pi101oo";
        using var cevap = await _istemci.PostAsJsonAsync(ApiYardimcilari.MusteriAdresi, govde);
        await cevap.DurumKoduDogrulaAsync(HttpStatusCode.Created);
        Assert.Equal("PI101OO", (await cevap.JsonOkuAsync()).GetProperty("postcode").GetString());
    }

    // §4.1 "En fazla": firstName 10 karakter; daha uzunu 400.
    [Fact]
    [Trait("Yoklama", "musteri-ekle")]
    public async Task Y02_uzun_ad_400_doner()
    {
        var govde = Musteri();
        govde["firstName"] = new string('A', 11);
        using var cevap = await _istemci.PostAsJsonAsync(ApiYardimcilari.MusteriAdresi, govde);
        await cevap.DurumKoduDogrulaAsync(HttpStatusCode.BadRequest);
        Assert.Equal("validation-error", await Tip(cevap));
    }

    // §3 bilinçli sapma: geçersiz doğum tarihi 400 (validation-error).
    [Fact]
    [Trait("Yoklama", "musteri-ekle")]
    public async Task Y03_gecersiz_dogum_tarihi_400_doner()
    {
        var govde = Musteri();
        govde["dateOfBirth"] = "31-02-1950";
        using var cevap = await _istemci.PostAsJsonAsync(ApiYardimcilari.MusteriAdresi, govde);
        await cevap.DurumKoduDogrulaAsync(HttpStatusCode.BadRequest);
        Assert.Equal("validation-error", await Tip(cevap));
    }

    // §1 "Yalnızca cevapta olan alanlar": istekteki customerNumber yok sayılır, istek reddedilmez.
    [Fact]
    [Trait("Yoklama", "musteri-ekle")]
    public async Task Y04_istekteki_musteri_numarasi_yok_sayilir()
    {
        var govde = Musteri().ToDictionary(a => a.Key, a => (object)a.Value);
        govde["customerNumber"] = 5;
        using var cevap = await _istemci.PostAsJsonAsync(ApiYardimcilari.MusteriAdresi, govde);
        await cevap.DurumKoduDogrulaAsync(HttpStatusCode.Created);
        Assert.NotEqual(5, ApiYardimcilari.MusteriNumarasiOku(await cevap.JsonOkuAsync()));
    }

    // §1 "Boş metin": boş gönderilen metin cevapta "" döner, null değil.
    [Fact]
    [Trait("Yoklama", "musteri-ekle")]
    public async Task Y05_bos_metin_cevapta_bos_doner()
    {
        var govde = Musteri();
        govde["phoneHome"] = "";
        using var cevap = await _istemci.PostAsJsonAsync(ApiYardimcilari.MusteriAdresi, govde);
        await cevap.DurumKoduDogrulaAsync(HttpStatusCode.Created);
        var j = await cevap.JsonOkuAsync();
        Assert.Equal(JsonValueKind.String, j.GetProperty("phoneHome").ValueKind);
        Assert.Equal("", j.GetProperty("phoneHome").GetString());
    }

    // §1 "Numaralar" + §3 "99" notu: sayı olmayan numara ASP.NET Core'un 404'ünü alır.
    [Fact]
    [Trait("Yoklama", "musteri-sorgula")]
    public async Task Y06_sayi_olmayan_numara_404_doner()
    {
        using var cevap = await _istemci.GetAsync(ApiYardimcilari.MusteriAdresi + "/abc");
        await cevap.DurumKoduDogrulaAsync(HttpStatusCode.NotFound);
    }

    // §3 bilinçli sapma (10 haneli numara): kesilmez; kayıt yoksa 404 / "01" (int sınırı içinde).
    [Fact]
    [Trait("Yoklama", "musteri-sorgula")]
    public async Task Y07_on_haneli_numara_kesilmez_404_01_doner()
    {
        using var cevap = await _istemci.GetAsync(ApiYardimcilari.MusteriAdresiNumarayla(2000000000));
        await cevap.HataGovdesiDogrulaAsync(HttpStatusCode.NotFound, "not-found", "01");
    }

    // §3 bilinçli sapma: kayıt yokken müşteri güncelle 404 / "01".
    [Fact]
    [Trait("Yoklama", "musteri-guncelle")]
    public async Task Y08_olmayan_musteriyi_guncelle_404_01_doner()
    {
        using var cevap = await _istemci.PutAsJsonAsync(ApiYardimcilari.MusteriAdresiNumarayla(999999999), Musteri());
        await cevap.HataGovdesiDogrulaAsync(HttpStatusCode.NotFound, "not-found", "01");
    }

    // §1 "Posta kodu": güncellemede de büyük harf; değer sonraki GET'te de büyük harf.
    [Fact]
    [Trait("Yoklama", "musteri-guncelle")]
    public async Task Y09_guncellemede_posta_kodu_buyuk_harfe_cevrilir()
    {
        var no = await _istemci.MusteriKurAsync(MusteriVerisi.AndrewPandy);
        var govde = Musteri();
        govde["postcode"] = "tb14tv";
        using var cevap = await _istemci.PutAsJsonAsync(ApiYardimcilari.MusteriAdresiNumarayla(no), govde);
        await cevap.DurumKoduDogrulaAsync(HttpStatusCode.OK);
        Assert.Equal("TB14TV", (await cevap.JsonOkuAsync()).GetProperty("postcode").GetString());
        using var oku = await _istemci.GetAsync(ApiYardimcilari.MusteriAdresiNumarayla(no));
        Assert.Equal("TB14TV", (await oku.JsonOkuAsync()).GetProperty("postcode").GetString());
    }

    // §4.1 "En fazla": lastName 20 karakter; daha uzunu 400.
    [Fact]
    [Trait("Yoklama", "musteri-guncelle")]
    public async Task Y10_guncellemede_uzun_soyad_400_doner()
    {
        var no = await _istemci.MusteriKurAsync(MusteriVerisi.AndrewPandy);
        var govde = Musteri();
        govde["lastName"] = new string('B', 21);
        using var cevap = await _istemci.PutAsJsonAsync(ApiYardimcilari.MusteriAdresiNumarayla(no), govde);
        await cevap.DurumKoduDogrulaAsync(HttpStatusCode.BadRequest);
        Assert.Equal("validation-error", await Tip(cevap));
    }

    // §4.3 "En fazla": make 15 karakter; daha uzunu 400.
    [Fact]
    [Trait("Yoklama", "motor-ekle")]
    public async Task Y11_uzun_marka_400_doner()
    {
        var no = await _istemci.MusteriKurAsync(MusteriVerisi.AndrewPandy);
        var govde = Motor();
        govde["make"] = new string('F', 16);
        using var cevap = await _istemci.PostAsJsonAsync(MotorYardimcilari.MotorAdresi(no), govde);
        await cevap.DurumKoduDogrulaAsync(HttpStatusCode.BadRequest);
        Assert.Equal("validation-error", await Tip(cevap));
    }

    // §1 "Yalnızca cevapta olan alanlar": istekteki policyNumber yok sayılır.
    [Fact]
    [Trait("Yoklama", "motor-ekle")]
    public async Task Y12_istekteki_police_numarasi_yok_sayilir()
    {
        var no = await _istemci.MusteriKurAsync(MusteriVerisi.AndrewPandy);
        var govde = Motor();
        govde["policyNumber"] = 7;
        using var cevap = await _istemci.PostAsJsonAsync(MotorYardimcilari.MotorAdresi(no), govde);
        await cevap.DurumKoduDogrulaAsync(HttpStatusCode.Created);
        Assert.NotEqual(7, MotorYardimcilari.PoliceNumarasiOku(await cevap.JsonOkuAsync()));
    }

    // §1 "Tarihler": yyyy-MM-dd; geçersiz tarih 400.
    [Fact]
    [Trait("Yoklama", "motor-ekle")]
    public async Task Y13_gecersiz_tarih_400_doner()
    {
        var no = await _istemci.MusteriKurAsync(MusteriVerisi.AndrewPandy);
        var govde = Motor();
        govde["issueDate"] = "11/05/2011";
        using var cevap = await _istemci.PostAsJsonAsync(MotorYardimcilari.MotorAdresi(no), govde);
        await cevap.DurumKoduDogrulaAsync(HttpStatusCode.BadRequest);
        Assert.Equal("validation-error", await Tip(cevap));
    }

    // §3 bilinçli sapma (poliçe başka türden): konut poliçesi motor adresinden sorgulanırsa 404 / "01".
    [Fact]
    [Trait("Yoklama", "motor-sorgula")]
    public async Task Y14_baska_turden_police_404_01_doner()
    {
        var no = await _istemci.MusteriKurAsync(MusteriVerisi.AndrewPandy);
        var konut = await _istemci.KonutPoliceKurAsync(no, KonutVerisi.Police6);
        using var cevap = await _istemci.GetAsync(MotorYardimcilari.MotorAdresiNumarayla(no, konut));
        await cevap.HataGovdesiDogrulaAsync(HttpStatusCode.NotFound, "not-found", "01");
    }

    // §1 "Numaralar": sayı olmayan poliçe numarası 404.
    [Fact]
    [Trait("Yoklama", "motor-sorgula")]
    public async Task Y15_sayi_olmayan_police_numarasi_404_doner()
    {
        var no = await _istemci.MusteriKurAsync(MusteriVerisi.AndrewPandy);
        using var cevap = await _istemci.GetAsync(MotorYardimcilari.MotorAdresi(no) + "/abc");
        await cevap.DurumKoduDogrulaAsync(HttpStatusCode.NotFound);
    }

    // §1 "Metinler": ekle'den dönen değerler sorguda da aynı ve kırpılmış.
    [Fact]
    [Trait("Yoklama", "motor-sorgula")]
    public async Task Y16_sorguda_alanlar_eklenenle_ayni_doner()
    {
        var no = await _istemci.MusteriKurAsync(MusteriVerisi.AndrewPandy);
        var police = await _istemci.MotorPoliceKurAsync(no, MotorVerisi.Ford);
        var j = await _istemci.MotorPoliceOkuAsync(no, police, "sorgu");
        Assert.Equal("FORD", j.GetProperty("make").GetString());
        Assert.Equal("LL60LOO", j.GetProperty("registrationNumber").GetString());
        Assert.Equal("BRK0000001", j.GetProperty("brokersReference").GetString());
    }
}
