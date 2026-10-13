using GenApp.Application.Policies;
using GenApp.Domain.Policies;
using GenApp.Infrastructure.Tests.Altyapi;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GenApp.Infrastructure.Tests.Policies;

/// <summary>
/// Poliçe sil (eski LGDPDB01): DELETE FROM POLICY WHERE müşteri no AND poliçe no (lgdpdb01.cbl:189-193); tür satırını
/// ON DELETE CASCADE siler. Kayıt yoksa 0 satır silinir; servis bunu eski koddaki gibi 90'a çevirir (:198-199).
/// :196-197'deki yorum "kayıt yoksa başarılı" der ama kod 90 döndürür; yeni sistem kodu izler (docs/api-design.md §3).
/// </summary>
[Collection(PostgresKoleksiyonu.Ad)]
public sealed class PolicyRepositoryDeleteTests(PostgresFixture veritabani)
{
    private async Task<int> SilAsync(int musteri, int police)
    {
        await using var kapsam = veritabani.Kapsam();
        return await kapsam.ServiceProvider.GetRequiredService<IPolicyRepository>().DeleteAsync(musteri, police);
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

        var silinen = await SilAsync(musteri, eklenen.PolicyNumber);

        Assert.Equal(1, silinen);
        Assert.False(await PoliceVarMiAsync(eklenen.PolicyNumber));
        Assert.False(await MotorVarMiAsync(eklenen.PolicyNumber));
    }

    [Fact]
    public async Task Baska_musterinin_numarasiyla_silme_hicbir_sey_silmez()
    {
        var sahibi = await veritabani.MusteriEkleAsync();
        var baskasi = await veritabani.MusteriEkleAsync();
        var eklenen = await veritabani.PoliceEkleAsync(sahibi, new MotorPolicy());

        var silinen = await SilAsync(baskasi, eklenen.PolicyNumber);

        Assert.Equal(0, silinen);
        Assert.True(await PoliceVarMiAsync(eklenen.PolicyNumber));
        Assert.True(await MotorVarMiAsync(eklenen.PolicyNumber));
    }

    [Fact]
    public async Task Olmayan_policeyi_silmek_sifir_satir_dondurur()
    {
        var musteri = await veritabani.MusteriEkleAsync();

        var silinen = await SilAsync(musteri, 999999999);

        Assert.Equal(0, silinen);
    }
}
