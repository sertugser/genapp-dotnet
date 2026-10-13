using GenApp.Application.Customers;
using GenApp.Domain.Customers;
using GenApp.Infrastructure.Tests.Altyapi;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GenApp.Infrastructure.Tests.Customers;

/// <summary>
/// Müşteri güncelle (eski LGUCDB01): tek UPDATE dokuz sütunun hepsini yazar (lgucdb01.cbl:152-169); boş değer
/// eskisini ezer. Satır yoksa güncellenen satır sayısı 0'dır; servis bunu '01'e çevirir (lgucdb01.cbl:173-174).
/// </summary>
[Collection(PostgresKoleksiyonu.Ad)]
public sealed class CustomerRepositoryUpdateTests(PostgresFixture veritabani)
{
    private async Task<int> GuncelleAsync(Customer musteri)
    {
        await using var kapsam = veritabani.Kapsam();
        return await kapsam.ServiceProvider.GetRequiredService<ICustomerRepository>().UpdateAsync(musteri);
    }

    [Fact]
    public async Task Dokuz_alanin_hepsi_yazilir_bos_deger_eskisini_ezer()
    {
        var numara = await veritabani.MusteriEkleAsync();
        var yeni = new Customer
        {
            CustomerNumber = numara,
            FirstName = "Scott",
            LastName = "Tracey",
            DateOfBirth = new DateOnly(1965, 9, 30),
            HouseName = "Tracey Island",
            HouseNumber = "1",
            Postcode = "TB14TV",
            PhoneMobile = "",
            PhoneHome = "001 911911",
            EmailAddress = "REFROOM@TBHOLDINGS.COM",
        };

        Assert.Equal(1, await GuncelleAsync(yeni));

        var okunan = await veritabani.OkuAsync(db => db.Customers.SingleAsync(c => c.CustomerNumber == numara));
        Assert.Equivalent(yeni, okunan, strict: true);
    }

    [Fact]
    public async Task Olmayan_musteride_hicbir_satir_guncellenmez()
    {
        Assert.Equal(0, await GuncelleAsync(new Customer { CustomerNumber = 999999999, FirstName = "X" }));
    }
}
