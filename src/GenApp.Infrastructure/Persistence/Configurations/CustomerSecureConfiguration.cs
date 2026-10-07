using GenApp.Domain.Customers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GenApp.Infrastructure.Persistence.Configurations;

public class CustomerSecureConfiguration : IEntityTypeConfiguration<CustomerSecure>
{
    public void Configure(EntityTypeBuilder<CustomerSecure> b)
    {
        b.ToTable("customer_secure");
        b.HasKey(s => s.CustomerNumber);
        b.Property(s => s.CustomerNumber).ValueGeneratedNever();

        b.Property(s => s.CustomerPass).HasMaxLength(32);
        b.Property(s => s.StateIndicator).HasMaxLength(1);

        b.HasOne<Customer>()
            .WithOne()
            .HasForeignKey<CustomerSecure>(s => s.CustomerNumber)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
