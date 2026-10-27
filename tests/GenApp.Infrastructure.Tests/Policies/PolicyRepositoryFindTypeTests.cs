using GenApp.Application.Policies;
using GenApp.Domain.Policies;
using GenApp.Infrastructure.Tests.Altyapi;
using Microsoft.Extensions.DependencyInjection;

namespace GenApp.Infrastructure.Tests.Policies;

/// <summary>
/// Poliçenin türünü okuma: silmeden önce servis, poliçenin adresteki türden olup olmadığına bakar. Eski LGDPDB01 türe
/// bakmadan siler (lgdpdb01.cbl:189-194); yeni sistemde tür uyuşmazsa hiçbir şey silinmez ve 01 döner, poliçe hiç
/// yoksa eski koddaki gibi 90 döner (docs/api-design.md §3, ADR 0002 Karar 9). Bu iki durumu ayırmak için tür ayrıca
/// okunur. Müşteri numarası da eşleşmeli: başka müşterinin poliçesi "yok" sayılır (DELETE'in WHERE'i, :189-193).
/// </summary>
[Collection(PostgresKoleksiyonu.Ad)]
public sealed class PolicyRepositoryFindTypeTests(PostgresFixture veritabani)
{
    private async Task<string?> TurBulAsync(int musteri, int police)
    {
        await using var kapsam = veritabani.Kapsam();
        return await kapsam.ServiceProvider.GetRequiredService<IPolicyRepository>().FindTypeAsync(musteri, police);
    }

    [Fact]
    public async Task Policenin_turu_doner()
    {
        var musteri = await veritabani.MusteriEkleAsync();
        var motor = await veritabani.PoliceEkleAsync(musteri, new MotorPolicy());
        var konut = await veritabani.PoliceEkleAsync(musteri, new HousePolicy());

        Assert.Equal("M", await TurBulAsync(musteri, motor.PolicyNumber));
        Assert.Equal("H", await TurBulAsync(musteri, konut.PolicyNumber));
    }

    [Fact]
    public async Task Baska_musterinin_numarasiyla_tur_bulunmaz()
    {
        var sahibi = await veritabani.MusteriEkleAsync();
        var baskasi = await veritabani.MusteriEkleAsync();
        var eklenen = await veritabani.PoliceEkleAsync(sahibi, new MotorPolicy());

        Assert.Null(await TurBulAsync(baskasi, eklenen.PolicyNumber));
    }

    [Fact]
    public async Task Olmayan_policenin_turu_bulunmaz()
    {
        var musteri = await veritabani.MusteriEkleAsync();

        Assert.Null(await TurBulAsync(musteri, 999999999));
    }
}
