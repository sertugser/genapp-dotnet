using GenApp.Application.Policies;
using GenApp.Domain.Policies;
using GenApp.Infrastructure.Tests.Altyapi;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GenApp.Infrastructure.Tests.Policies;

/// <summary>
/// Poliçe sil (eski LGDPDB01): DELETE FROM POLICY WHERE müşteri no AND poliçe no (lgdpdb01.cbl:189-193); tür satırını
/// ON DELETE CASCADE siler. Kayıt yoksa da başarılıdır: "end result is record does not exist" (:196-197).
/// </summary>
[Collection(PostgresKoleksiyonu.Ad)]
public sealed class PolicyRepositoryDeleteTests(PostgresFixture veritabani)
{
    private async Task SilAsync(int musteri, int police)
    {
        await using var kapsam = veritabani.Kapsam();
        await kapsam.ServiceProvider.GetRequiredService<IPolicyRepository>().DeleteAsync(musteri, police);
    }

    private Task<bool> PoliceVarMiAsync(int police) =>
        veritabani.OkuAsync(db => db.Policies.AnyAsync(p => p.PolicyNumber == police));

    private Task<bool> MotorVarMiAsync(int police) =>
        veritabani.OkuAsync(db => db.MotorPolicies.AnyAsync(m => m.PolicyNumber == police));

    [Fact]
    public async Task Police_ve_tur_satiri_silinir()
    {
        var musteri = await veritabani.MusteriEkleAsync();
        var eklenen = await veritabani.PoliceEkleAsync(musteri, new MotorPolicy());

        await SilAsync(musteri, eklenen.PolicyNumber);

        Assert.False(await PoliceVarMiAsync(eklenen.PolicyNumber));
        Assert.False(await MotorVarMiAsync(eklenen.PolicyNumber));
    }

    [Fact]
    public async Task Baska_musterinin_numarasiyla_silme_hicbir_sey_silmez()
    {
        var sahibi = await veritabani.MusteriEkleAsync();
        var baskasi = await veritabani.MusteriEkleAsync();
        var eklenen = await veritabani.PoliceEkleAsync(sahibi, new MotorPolicy());

        await SilAsync(baskasi, eklenen.PolicyNumber);

        Assert.True(await PoliceVarMiAsync(eklenen.PolicyNumber));
        Assert.True(await MotorVarMiAsync(eklenen.PolicyNumber));
    }

    [Fact]
    public async Task Olmayan_policeyi_silmek_hata_vermez()
    {
        var musteri = await veritabani.MusteriEkleAsync();

        var hata = await Record.ExceptionAsync(() => SilAsync(musteri, 999999999));

        Assert.Null(hata);
    }
}
