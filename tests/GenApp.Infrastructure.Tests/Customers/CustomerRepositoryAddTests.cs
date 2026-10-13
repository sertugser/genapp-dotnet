using GenApp.Application.Customers;
using GenApp.Application.Persistence;
using GenApp.Domain.Customers;
using GenApp.Infrastructure.Tests.Altyapi;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GenApp.Infrastructure.Tests.Customers;

/// <summary>
/// Müşteri ekle (eski LGACDB01 + LGACDB02): CUSTOMER'a satır eklenir, numarayı veritabanı üretir
/// (lgacdb01.cbl:251-282); aynı numarayla CUSTOMER_SECURE'a satır yazılır (lgacdb02.cbl:143-182).
/// İkisi tek transaction'dadır (ADR 0003 Karar 2): commit edilmezse hiçbiri kalmaz.
/// </summary>
[Collection(PostgresKoleksiyonu.Ad)]
public sealed class CustomerRepositoryAddTests(PostgresFixture veritabani)
{
    private async Task<int> EkleAsync(bool onayla)
    {
        await using var kapsam = veritabani.Kapsam();
        var musteriler = kapsam.ServiceProvider.GetRequiredService<ICustomerRepository>();
        var birim = kapsam.ServiceProvider.GetRequiredService<IUnitOfWork>();

        await using var islem = await birim.BeginTransactionAsync();
        var musteri = new Customer { FirstName = "Andrew", LastName = "Pandy" };
        musteriler.Add(musteri);
        await birim.SaveChangesAsync();
        musteriler.AddSecurity(new CustomerSecure { CustomerNumber = musteri.CustomerNumber, CustomerPass = "x", StateIndicator = "N", PassChanges = 0 });
        await birim.SaveChangesAsync();

        if (onayla)
        {
            await islem.CommitAsync();
        }

        return musteri.CustomerNumber;
    }

    [Fact]
    public async Task Musteri_ve_guvenlik_satiri_ayni_numarayla_eklenir()
    {
        var numara = await EkleAsync(onayla: true);

        Assert.True(numara >= 1000001);
        Assert.True(await veritabani.OkuAsync(db => db.Customers.AnyAsync(c => c.CustomerNumber == numara)));
        Assert.True(await veritabani.OkuAsync(db => db.CustomerSecures.AnyAsync(s => s.CustomerNumber == numara)));
    }

    [Fact]
    public async Task Onaylanmayan_islemde_hicbir_satir_kalmaz()
    {
        var numara = await EkleAsync(onayla: false);

        Assert.False(await veritabani.OkuAsync(db => db.Customers.AnyAsync(c => c.CustomerNumber == numara)));
        Assert.False(await veritabani.OkuAsync(db => db.CustomerSecures.AnyAsync(s => s.CustomerNumber == numara)));
    }
}
