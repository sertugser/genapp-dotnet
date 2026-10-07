using GenApp.Domain.Customers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GenApp.Infrastructure.Persistence.Configurations;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> b)
    {
        b.ToTable("customer");
        b.HasKey(c => c.CustomerNumber);

        b.Property(c => c.CustomerNumber)
            .UseIdentityByDefaultColumn()
            .HasIdentityOptions(startValue: 1000001);

        b.Property(c => c.FirstName).HasMaxLength(10);
        b.Property(c => c.LastName).HasMaxLength(20);
        b.Property(c => c.HouseName).HasMaxLength(20);
        b.Property(c => c.HouseNumber).HasMaxLength(4);
        b.Property(c => c.Postcode).HasMaxLength(8);
        b.Property(c => c.PhoneHome).HasMaxLength(20);
        b.Property(c => c.PhoneMobile).HasMaxLength(20);
        b.Property(c => c.EmailAddress).HasMaxLength(100);
    }
}
