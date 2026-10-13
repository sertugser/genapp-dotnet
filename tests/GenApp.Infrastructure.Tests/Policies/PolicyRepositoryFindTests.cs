using GenApp.Application.Policies;
using GenApp.Domain.Policies;
using GenApp.Infrastructure.Tests.Altyapi;
using Microsoft.Extensions.DependencyInjection;

namespace GenApp.Infrastructure.Tests.Policies;

/// <summary>
/// Poliçe sorgula (eski LGIPDB01): POLICY ile tür tablosu birleştirilir, müşteri no VE poliçe no ile aranır
/// (lgipdb01.cbl motor sorgusu, POLICY.CUSTOMERNUMBER = ... AND POLICY.POLICYNUMBER = ...). Bulunamazsa '01'.
/// </summary>
[Collection(PostgresKoleksiyonu.Ad)]
public sealed class PolicyRepositoryFindTests(PostgresFixture veritabani)
{
    private async Task<PolicyRecord<T>?> BulAsync<T>(int musteri, int police) where T : class, IPolicyDetails
    {
        await using var kapsam = veritabani.Kapsam();
        return await kapsam.ServiceProvider.GetRequiredService<IPolicyRepository>().FindAsync<T>(musteri, police);
    }

    [Fact]
    public async Task Dogru_musteri_ve_police_numarasiyla_police_ve_tur_satiri_birlikte_gelir()
    {
        var musteri = await veritabani.MusteriEkleAsync();
        var eklenen = await veritabani.PoliceEkleAsync(musteri, new MotorPolicy { Make = "FORD", Model = "KA" });

        var kayit = await BulAsync<MotorPolicy>(musteri, eklenen.PolicyNumber);

        Assert.NotNull(kayit);
        Assert.Equal(musteri, kayit.Policy.CustomerNumber);
        Assert.Equal("BR1", kayit.Policy.BrokersReference);
        Assert.Equal(new DateTime(2026, 10, 13, 14, 5, 9).AddTicks(1234560), kayit.Policy.LastChanged);
        Assert.Equal("FORD", kayit.Details.Make);
        Assert.Equal("KA", kayit.Details.Model);
    }

    [Fact]
    public async Task Baska_musterinin_numarasiyla_sorulan_police_bulunmaz()
    {
        var sahibi = await veritabani.MusteriEkleAsync();
        var baskasi = await veritabani.MusteriEkleAsync();
        var eklenen = await veritabani.PoliceEkleAsync(sahibi, new MotorPolicy());

        Assert.Null(await BulAsync<MotorPolicy>(baskasi, eklenen.PolicyNumber));
    }

    [Fact]
    public async Task Olmayan_police_bulunmaz()
    {
        var musteri = await veritabani.MusteriEkleAsync();

        Assert.Null(await BulAsync<MotorPolicy>(musteri, 999999999));
    }

    // 01IHOU ile bir motor poliçesi sorulursa LGIPDB01 HOUSE tablosuyla birleştirir ve satır bulamaz.
    [Fact]
    public async Task Motor_policesi_konut_olarak_sorulursa_bulunmaz()
    {
        var musteri = await veritabani.MusteriEkleAsync();
        var eklenen = await veritabani.PoliceEkleAsync(musteri, new MotorPolicy());

        Assert.Null(await BulAsync<HousePolicy>(musteri, eklenen.PolicyNumber));
    }
}
