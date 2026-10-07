using GenApp.Domain.Policies;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GenApp.Infrastructure.Persistence.Configurations;

public class HousePolicyConfiguration : IEntityTypeConfiguration<HousePolicy>
{
    public void Configure(EntityTypeBuilder<HousePolicy> b)
    {
        b.ToTable("house");
        b.HasKey(h => h.PolicyNumber);
        b.Property(h => h.PolicyNumber).ValueGeneratedNever();

        b.Property(h => h.PropertyType).HasMaxLength(15);
        b.Property(h => h.HouseName).HasMaxLength(20);
        b.Property(h => h.HouseNumber).HasMaxLength(4);
        b.Property(h => h.Postcode).HasMaxLength(8);

        b.HasOne<Policy>()
            .WithOne()
            .HasForeignKey<HousePolicy>(h => h.PolicyNumber)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
