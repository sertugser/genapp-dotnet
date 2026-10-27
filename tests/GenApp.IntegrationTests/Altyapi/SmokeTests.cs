using System.Net;
using System.Text;
using Xunit.Abstractions;

namespace GenApp.IntegrationTests.Altyapi;

/// <summary>
/// Altyapının duman testi: fixture API'yi ayağa kaldırıp veritabanına bağlanabiliyor mu, şema docs/database-schema.md'deki
/// tabloları ve <see cref="Sema"/>'daki sütunları içeriyor mu. Operasyon testi değildir; <c>Operasyon</c> etiketi taşımaz.
/// </summary>
/// <remarks>
/// Sütun sınıfları (<see cref="SutunDurumu"/>): <c>Kesin</c> bir sütun bulunamazsa test kırmızı olur. <c>SmokeaBagli</c>
/// bir sütun bulunamazsa test kırmızı olmaz; "BELGE BELİRSİZ" olarak raporlanır ve o alan dilim testlerinde veritabanında
/// doğrulanmaz. <c>Dogrulanmaz</c> sütunların yalnızca varlığı raporlanır. Gerçek şemadaki bütün tablo ve sütunlar ile
/// <see cref="Sema"/>'da olmayan sütunlar da test çıktısına yazılır; bunlar bilgi içindir, beklentiye çevrilmez.
/// </remarks>
[Collection(Koleksiyon.Altyapi)]
public sealed class SmokeTests(EntegrasyonFixture fixture, ITestOutputHelper cikti)
{
    /// <summary>
    /// API'nin veritabanına giden bir yolu baştan sona çalışıyor mu: olmayan müşteriyi sorgulamak bir veritabanı okuması
    /// gerektirir. Beklenen: 404, <c>not-found</c>, <c>"01"</c> (AD:36, AD:94). 999999999 atanmayan numara (DS:49-50;
    /// ADR 0005 02-02).
    /// </summary>
    [Fact]
    public async Task Fixture_APIyi_ayaga_kaldirir_ve_API_veritabanina_ulasir()
    {
        using var cevap = await fixture.Istemci.GetAsync(ApiYardimcilari.MusteriAdresiNumarayla(999999999));

        await cevap.HataGovdesiDogrulaAsync(HttpStatusCode.NotFound, "not-found", "01");
    }

    /// <summary>Doğrulama yolu (ham SQL) aynı veritabanına bağlanıyor ve DS:10-19'daki sekiz tablo, Kesin sütunlar var.</summary>
    [Fact]
    public async Task Beklenen_tablolar_ve_kesin_sutunlar_semada_var()
    {
        var tablolar = await fixture.Veritabani.TablolariOkuAsync();
        var sutunlar = await fixture.Veritabani.SutunlariOkuAsync();

        var rapor = new StringBuilder();

        rapor.AppendLine("== Gerçek şema (information_schema, current_schema()) ==");
        foreach (var tablo in tablolar.Order())
        {
            var ad = sutunlar.TryGetValue(tablo, out var s) ? string.Join(", ", s.Order()) : "(sütun yok)";
            rapor.AppendLine($"  {tablo}: {ad}");
        }

        var eksikTablolar = Sema.BeklenenTablolar.Where(t => !tablolar.Contains(t)).ToList();
        rapor.AppendLine("== Bulunamayan tablolar ==");
        Yaz(rapor, eksikTablolar);

        bool Var(BeklenenSutun b) => sutunlar.TryGetValue(b.Tablo, out var s) && s.Contains(b.Ad);

        var eksikKesin = Sema.BeklenenSutunlar.Where(b => b.Durum == SutunDurumu.Kesin && !Var(b)).ToList();
        var eksikSmoke = Sema.BeklenenSutunlar.Where(b => b.Durum == SutunDurumu.SmokeaBagli && !Var(b)).ToList();

        rapor.AppendLine("== Bulunamayan Kesin sütunlar (test kırmızı) ==");
        Yaz(rapor, eksikKesin.Select(Goster));
        rapor.AppendLine("== Bulunamayan SmokeaBagli sütunlar: BELGE BELİRSİZ (veritabanında doğrulanmayacak) ==");
        Yaz(rapor, eksikSmoke.Select(Goster));
        rapor.AppendLine("== Bulunan SmokeaBagli sütunlar (veritabanında doğrulanacak) ==");
        Yaz(rapor, Sema.BeklenenSutunlar.Where(b => b.Durum == SutunDurumu.SmokeaBagli && Var(b)).Select(Goster));
        rapor.AppendLine("== Dogrulanmaz sütunlar (yalnızca varlık) ==");
        Yaz(rapor, Sema.BeklenenSutunlar
            .Where(b => b.Durum == SutunDurumu.Dogrulanmaz)
            .Select(b => $"{Goster(b)}: {(Var(b) ? "var" : "YOK")}"));

        var beklenenAdlar = Sema.BeklenenSutunlar.Select(b => (b.Tablo, b.Ad)).ToHashSet();
        rapor.AppendLine("== Gerçek şemada olup Sema.cs'de olmayan sütunlar (bilgi; beklentiye çevrilmez) ==");
        Yaz(rapor, sutunlar
            .Where(t => Sema.BeklenenTablolar.Contains(t.Key) && t.Key != Sema.ClaimTablosu)
            .SelectMany(t => t.Value.Where(s => !beklenenAdlar.Contains((t.Key, s))).Select(s => $"{t.Key}.{s}"))
            .Order());
        rapor.AppendLine("== Gerçek şemada olup belgede olmayan tablolar (bilgi) ==");
        Yaz(rapor, tablolar.Where(t => !Sema.BeklenenTablolar.Contains(t)).Order());

        cikti.WriteLine(rapor.ToString());

        Assert.True(
            eksikTablolar.Count == 0 && eksikKesin.Count == 0,
            $"Şema belgeyle uyuşmuyor. Bulunamayan tablolar: [{string.Join(", ", eksikTablolar)}]; bulunamayan Kesin " +
            $"sütunlar: [{string.Join(", ", eksikKesin.Select(Goster))}].\n{rapor}");
    }

    private static string Goster(BeklenenSutun b) => $"{b.Tablo}.{b.Ad} ({b.Kaynak})";

    private static void Yaz(StringBuilder rapor, IEnumerable<string> satirlar)
    {
        var liste = satirlar.ToList();
        if (liste.Count == 0)
        {
            rapor.AppendLine("  (yok)");
        }
        foreach (var satir in liste)
        {
            rapor.AppendLine($"  {satir}");
        }
    }
}
