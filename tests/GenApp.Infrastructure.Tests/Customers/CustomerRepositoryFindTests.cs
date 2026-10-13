using GenApp.Application.Customers;
using GenApp.Domain.Customers;
using GenApp.Infrastructure.Persistence;
using GenApp.Infrastructure.Tests.Altyapi;
using Microsoft.Extensions.DependencyInjection;

namespace GenApp.Infrastructure.Tests.Customers;

/// <summary>
/// Müşteri sorgula (eski LGICDB01): CUSTOMER tablosundan müşteri numarasıyla dokuz sütun okunur
/// (lgicdb01.cbl:169-190). Satır yoksa repository null döner; servis bunu '01'e çevirir (lgicdb01.cbl:195-196).
/// </summary>
[Collection(PostgresKoleksiyonu.Ad)]
public sealed class CustomerRepositoryFindTests(PostgresFixture veritabani)
{
    private async Task<Customer?> BulAsync(int musteriNumarasi)
    {
        await using var kapsam = veritabani.Kapsam();
        return await kapsam.ServiceProvider.GetRequiredService<ICustomerRepository>().FindAsync(musteriNumarasi);
    }

    [Fact]
    public async Task Var_olan_musteri_dokuz_alaniyla_gelir()
    {
        var eklenen = new Customer
        {
            FirstName = "Andrew",
            LastName = "Pandy",
            DateOfBirth = new DateOnly(1950, 7, 11),
            HouseName = "",
            HouseNumber = "34",
            Postcode = "PI101OO",
            PhoneMobile = "07799 123456",
            PhoneHome = "01962 811234",
            EmailAddress = "A.Pandy@beebhouse.com",
        };
        await using (var kapsam = veritabani.Kapsam())
        {
            var db = kapsam.ServiceProvider.GetRequiredService<GenAppDbContext>();
            db.Customers.Add(eklenen);
            await db.SaveChangesAsync();
        }

        var musteri = await BulAsync(eklenen.CustomerNumber);

        Assert.NotNull(musteri);
        Assert.Equivalent(eklenen, musteri, strict: true);
    }

    [Fact]
    public async Task Olmayan_musteri_bulunmaz()
    {
        Assert.Null(await BulAsync(999999999));
    }
}
