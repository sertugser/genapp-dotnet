using GenApp.Application.Customers;
using GenApp.Application.Persistence;
using GenApp.Application.Policies;
using GenApp.Domain.Customers;
using GenApp.Domain.Policies;
using Microsoft.Extensions.DependencyInjection;

namespace GenApp.Infrastructure.Tests;

/// <summary>
/// Veritabanı hatası repository'den Application'ın <see cref="DatabaseException"/> tipine çevrilmiş olarak çıkar; hiçbir
/// EF Core ya da Npgsql tipi Infrastructure'ın dışına sızmaz (ADR 0001 Karar 3). Api bu tipi 500 / "90" ya da (deadlock,
/// timeout) 503'e çevirir (ADR 0002 Karar 8: her operasyonda). Eski veri programları da beklenmeyen her SQLCODE'da 90
/// döner (örneğin lgipdb01.cbl:424-428). Hata, hiçbir şeyin dinlemediği bir porta bağlanarak üretilir; gerçek
/// PostgreSQL gerekmez.
/// </summary>
public sealed class VeritabaniHatasiTests : IAsyncLifetime
{
    private const string UlasilamayanVeritabani =
        "Host=localhost;Port=1;Database=genapp_test;Username=genapp;Password=genapp_dev;Timeout=2";

    private ServiceProvider _servisler = null!;
    private AsyncServiceScope _kapsam;

    public Task InitializeAsync()
    {
        var servisler = new ServiceCollection();
        servisler.AddInfrastructure(UlasilamayanVeritabani);
        _servisler = servisler.BuildServiceProvider();
        _kapsam = _servisler.CreateAsyncScope();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await _kapsam.DisposeAsync();
        await _servisler.DisposeAsync();
    }

    private IPolicyRepository Policeler => _kapsam.ServiceProvider.GetRequiredService<IPolicyRepository>();

    private ICustomerRepository Musteriler => _kapsam.ServiceProvider.GetRequiredService<ICustomerRepository>();

    [Fact]
    public async Task Police_sorgulamada_veritabani_hatasi_cevrilir()
    {
        await Assert.ThrowsAsync<DatabaseException>(() => Policeler.FindAsync<MotorPolicy>(1, 1));
    }

    [Fact]
    public async Task Police_turu_okumada_veritabani_hatasi_cevrilir()
    {
        await Assert.ThrowsAsync<DatabaseException>(() => Policeler.FindTypeAsync(1, 1));
    }

    [Fact]
    public async Task Police_silmede_veritabani_hatasi_cevrilir()
    {
        await Assert.ThrowsAsync<DatabaseException>(() => Policeler.DeleteAsync(1, 1));
    }

    [Fact]
    public async Task Musteri_sorgulamada_veritabani_hatasi_cevrilir()
    {
        await Assert.ThrowsAsync<DatabaseException>(() => Musteriler.FindAsync(1));
    }

    [Fact]
    public async Task Musteri_guncellemede_veritabani_hatasi_cevrilir()
    {
        await Assert.ThrowsAsync<DatabaseException>(() => Musteriler.UpdateAsync(new Customer { CustomerNumber = 1 }));
    }
}
