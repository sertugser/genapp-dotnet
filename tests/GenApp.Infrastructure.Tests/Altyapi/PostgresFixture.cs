using GenApp.Application.Persistence;
using GenApp.Application.Policies;
using GenApp.Domain.Customers;
using GenApp.Domain.Policies;
using GenApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GenApp.Infrastructure.Tests.Altyapi;

/// <summary>
/// Testler için gerçek PostgreSQL. Geliştirme veritabanına (genapp) dokunmamak için ayrı bir genapp_test
/// veritabanı her çalıştırmada silinip migration'larla yeniden kurulur; model ile migration özeti uyuşmazsa
/// kurulum burada hata verir. Önce: <c>docker compose up -d</c>.
/// Servisler uygulamadaki gibi <c>AddInfrastructure</c> ile kaydedilir; yalnızca saat sabitlenir.
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private const string VarsayilanBaglanti =
        "Host=localhost;Port=5432;Database=genapp_test;Username=genapp;Password=genapp_dev";

    private ServiceProvider _servisler = null!;
    private ServiceProvider _sonrakiServisler = null!;

    // 7. ondalık hane (100 ns) bilerek dolu: PostgreSQL mikrosaniye tutar, bu hane kırpılmalıdır (ADR 0003 Karar 3).
    public SabitSaat Saat { get; } = new(new DateTime(2026, 10, 13, 14, 5, 9).AddTicks(1234567));

    // Güncellemenin yeni zaman damgasını eskisinden ayırt edebilmek için ikinci, daha geç bir saat.
    public SabitSaat SonrakiSaat { get; } = new(new DateTime(2026, 10, 14, 9, 30, 0).AddTicks(7654321));

    public async Task InitializeAsync()
    {
        var baglanti = Environment.GetEnvironmentVariable("ConnectionStrings__GenAppTest") ?? VarsayilanBaglanti;
        _servisler = Servisler(baglanti, Saat);
        _sonrakiServisler = Servisler(baglanti, SonrakiSaat);

        await using var kapsam = Kapsam();
        var db = kapsam.ServiceProvider.GetRequiredService<GenAppDbContext>();
        await db.Database.EnsureDeletedAsync();
        await db.Database.MigrateAsync();
    }

    private static ServiceProvider Servisler(string baglanti, TimeProvider saat)
    {
        var servisler = new ServiceCollection();
        servisler.AddInfrastructure(baglanti);
        servisler.AddSingleton(saat);
        return servisler.BuildServiceProvider();
    }

    public async Task DisposeAsync()
    {
        await _servisler.DisposeAsync();
        await _sonrakiServisler.DisposeAsync();
    }

    // Uygulamadaki bir HTTP isteğinin karşılığı: kapsam içindeki repository ve unit of work aynı DbContext'i paylaşır.
    public AsyncServiceScope Kapsam() => _servisler.CreateAsyncScope();

    // Aynı veritabanı, saat SonrakiSaat.
    public AsyncServiceScope SonrakiKapsam() => _sonrakiServisler.CreateAsyncScope();

    public async Task<int> MusteriEkleAsync()
    {
        await using var kapsam = Kapsam();
        var db = kapsam.ServiceProvider.GetRequiredService<GenAppDbContext>();
        var musteri = new Customer { FirstName = "Test", LastName = "Musteri" };
        db.Customers.Add(musteri);
        await db.SaveChangesAsync();
        return musteri.CustomerNumber;
    }

    public async Task<Policy> PoliceEkleAsync(int musteriNumarasi, IPolicyDetails tur)
    {
        await using var kapsam = Kapsam();
        var police = new Policy
        {
            CustomerNumber = musteriNumarasi,
            IssueDate = new DateOnly(2026, 1, 1),
            ExpiryDate = new DateOnly(2027, 1, 1),
            BrokerId = 42,
            BrokersReference = "BR1",
            Payment = 100,
        };

        kapsam.ServiceProvider.GetRequiredService<IPolicyRepository>().Add(police, tur);
        await kapsam.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync();
        return police;
    }

    // Repository'den bağımsız doğrulama: ayrı bir kapsamda veritabanından doğrudan okur.
    public async Task<T> OkuAsync<T>(Func<GenAppDbContext, Task<T>> sorgu)
    {
        await using var kapsam = Kapsam();
        return await sorgu(kapsam.ServiceProvider.GetRequiredService<GenAppDbContext>());
    }
}

[CollectionDefinition(Ad)]
public sealed class PostgresKoleksiyonu : ICollectionFixture<PostgresFixture>
{
    public const string Ad = "PostgreSQL";
}
