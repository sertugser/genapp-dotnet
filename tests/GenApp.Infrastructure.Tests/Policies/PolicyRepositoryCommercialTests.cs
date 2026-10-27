using GenApp.Application.Policies;
using GenApp.Domain.Policies;
using GenApp.Infrastructure.Tests.Altyapi;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GenApp.Infrastructure.Tests.Policies;

/// <summary>
/// Ticari poliçeye özgü veri erişimi. Ekleme tarihleri COMMERCIAL'a da yazar (eski LGAPDB01, 16-commercial-add.md not 2);
/// 02ICOM, 03ICOM ve 05ICOM sorguları (eski LGIPDB01, lgipdb01.cbl:778-781, :89-118, :120-149).
/// </summary>
[Collection(PostgresKoleksiyonu.Ad)]
public sealed class PolicyRepositoryCommercialTests(PostgresFixture veritabani)
{
    private async Task<T> RepoAsync<T>(Func<IPolicyRepository, Task<T>> islem)
    {
        await using var kapsam = veritabani.Kapsam();
        return await islem(kapsam.ServiceProvider.GetRequiredService<IPolicyRepository>());
    }

    [Fact]
    public async Task Eklemede_tarihler_ve_zaman_damgasi_commercial_tablosuna_da_yazilir()
    {
        var musteri = await veritabani.MusteriEkleAsync();
        var police = await veritabani.PoliceEkleAsync(musteri, new CommercialPolicy { Zipcode = "SO212JN" });

        var satir = await veritabani.OkuAsync(db => db.CommercialPolicies.SingleAsync(c => c.PolicyNumber == police.PolicyNumber));

        Assert.Equal(police.IssueDate, satir.StartDate);
        Assert.Equal(police.ExpiryDate, satir.RenewalDate);
        Assert.Equal(police.LastChanged, satir.RequestDate);
    }

    [Fact]
    public async Task Yalnizca_police_numarasiyla_sahibinden_bagimsiz_bulunur()
    {
        var musteri = await veritabani.MusteriEkleAsync();
        var police = await veritabani.PoliceEkleAsync(musteri, new CommercialPolicy { Customer = "IBM" });

        var kayit = await RepoAsync(r => r.FindCommercialAsync(police.PolicyNumber));

        Assert.NotNull(kayit);
        Assert.Equal(musteri, kayit.Policy.CustomerNumber);
        Assert.Equal("IBM", kayit.Details.Customer);
    }

    [Fact]
    public async Task Musteri_numarasiyla_yalnizca_o_musterinin_ticari_policeleri_gelir()
    {
        var musteri = await veritabani.MusteriEkleAsync();
        var baskasi = await veritabani.MusteriEkleAsync();
        var birinci = await veritabani.PoliceEkleAsync(musteri, new CommercialPolicy());
        var ikinci = await veritabani.PoliceEkleAsync(musteri, new CommercialPolicy());
        await veritabani.PoliceEkleAsync(musteri, new MotorPolicy());
        await veritabani.PoliceEkleAsync(baskasi, new CommercialPolicy());

        var liste = await RepoAsync(r => r.FindCommercialByCustomerAsync(musteri));

        Assert.Equal(
            new[] { birinci.PolicyNumber, ikinci.PolicyNumber }.Order(),
            liste.Select(k => k.Policy.PolicyNumber).Order());
    }

    [Fact]
    public async Task Posta_koduyla_birebir_esleyen_policeler_gelir()
    {
        var musteri = await veritabani.MusteriEkleAsync();
        var kod = $"T{Guid.NewGuid():N}"[..8].ToUpperInvariant();
        var police = await veritabani.PoliceEkleAsync(musteri, new CommercialPolicy { Zipcode = kod });
        await veritabani.PoliceEkleAsync(musteri, new CommercialPolicy { Zipcode = kod.ToLowerInvariant() });

        var liste = await RepoAsync(r => r.FindCommercialByPostcodeAsync(kod));

        Assert.Equal(police.PolicyNumber, Assert.Single(liste).Policy.PolicyNumber);
    }

    [Fact]
    public async Task Kayit_yoksa_bos_liste_doner()
    {
        Assert.Empty(await RepoAsync(r => r.FindCommercialByCustomerAsync(999999999)));
        Assert.Empty(await RepoAsync(r => r.FindCommercialByPostcodeAsync("YOK00000")));
    }
}
