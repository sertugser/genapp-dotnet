using GenApp.Application.Persistence;
using GenApp.Application.Policies;
using GenApp.Domain.Policies;
using GenApp.Infrastructure.Tests.Altyapi;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GenApp.Infrastructure.Tests.Policies;

/// <summary>
/// Poliçe güncelle (eski LGUPDB01) ve iyimser kilitleme. İstemcinin gönderdiği LASTCHANGED kayıttakiyle aynıysa
/// güncellenir ve zaman damgası yenilenir (lgupdb01.cbl:278, :322); farklıysa '02' (:346) ve hiçbir şey yazılmaz.
/// ADR 0003 Karar 3, NFR-05. Güncellemeler SonrakiSaat ile yapılır: 2026-10-14 09:30:00.7654321.
/// </summary>
[Collection(PostgresKoleksiyonu.Ad)]
public sealed class PolicyRepositoryUpdateTests(PostgresFixture veritabani)
{
    private static readonly DateTime YeniZamanDamgasi = new DateTime(2026, 10, 14, 9, 30, 0).AddTicks(7654320);

    private static async Task GuncelleAsync(AsyncServiceScope kapsam, int musteri, int police, string marka, DateTime istemcininLastChanged)
    {
        var repository = kapsam.ServiceProvider.GetRequiredService<IPolicyRepository>();
        var kayit = await repository.FindAsync<MotorPolicy>(musteri, police);
        kayit!.Details.Make = marka;
        kayit.Policy.ExpiryDate = new DateOnly(2028, 1, 1);
        repository.MarkUpdated(kayit.Policy, istemcininLastChanged);
        await kapsam.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync();
    }

    [Fact]
    public async Task Dogru_last_changed_ile_iki_tablo_guncellenir_ve_zaman_damgasi_yenilenir()
    {
        var musteri = await veritabani.MusteriEkleAsync();
        var eklenen = await veritabani.PoliceEkleAsync(musteri, new MotorPolicy { Make = "FORD" });

        await using (var kapsam = veritabani.SonrakiKapsam())
        {
            await GuncelleAsync(kapsam, musteri, eklenen.PolicyNumber, "VW", eklenen.LastChanged);
        }

        var motor = await veritabani.OkuAsync(db => db.MotorPolicies.AsNoTracking().SingleAsync(m => m.PolicyNumber == eklenen.PolicyNumber));
        var kayit = await veritabani.OkuAsync(db => db.Policies.AsNoTracking().SingleAsync(p => p.PolicyNumber == eklenen.PolicyNumber));
        Assert.Equal("VW", motor.Make);
        Assert.Equal(new DateOnly(2028, 1, 1), kayit.ExpiryDate);
        Assert.Equal(YeniZamanDamgasi, kayit.LastChanged);
    }

    // İki çalışan aynı poliçeyi aynı LASTCHANGED ile açar. A kaydeder; B'nin elindeki değer artık eskidir.
    [Fact]
    public async Task Ayni_eski_last_changed_ile_ikinci_guncelleme_ConcurrencyConflictException_firlatir_ve_yazmaz()
    {
        var musteri = await veritabani.MusteriEkleAsync();
        var eklenen = await veritabani.PoliceEkleAsync(musteri, new MotorPolicy { Make = "FORD" });
        var ikisininGordugu = eklenen.LastChanged;

        await using (var kapsamA = veritabani.SonrakiKapsam())
        {
            await GuncelleAsync(kapsamA, musteri, eklenen.PolicyNumber, "VW", ikisininGordugu);
        }

        await using (var kapsamB = veritabani.SonrakiKapsam())
        {
            await Assert.ThrowsAsync<ConcurrencyConflictException>(
                () => GuncelleAsync(kapsamB, musteri, eklenen.PolicyNumber, "OPEL", ikisininGordugu));
        }

        var motor = await veritabani.OkuAsync(db => db.MotorPolicies.AsNoTracking().SingleAsync(m => m.PolicyNumber == eklenen.PolicyNumber));
        var kayit = await veritabani.OkuAsync(db => db.Policies.AsNoTracking().SingleAsync(p => p.PolicyNumber == eklenen.PolicyNumber));
        Assert.Equal("VW", motor.Make);
        Assert.Equal(YeniZamanDamgasi, kayit.LastChanged);
    }
}
