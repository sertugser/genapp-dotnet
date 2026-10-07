using GenApp.Domain.Customers;
using GenApp.Domain.Policies;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GenApp.Infrastructure.Persistence.Configurations;

public class PolicyConfiguration : IEntityTypeConfiguration<Policy>
{
    public void Configure(EntityTypeBuilder<Policy> b)
    {
        b.ToTable("policy");
        b.HasKey(p => p.PolicyNumber);

        b.Property(p => p.PolicyNumber)
            .UseIdentityByDefaultColumn()
            .HasIdentityOptions(startValue: 1000001);

        b.Property(p => p.PolicyType).HasMaxLength(1);
        b.Property(p => p.BrokersReference).HasMaxLength(10);

        b.Property(p => p.LastChanged)
            .HasColumnType("timestamp without time zone")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsConcurrencyToken();

        b.HasOne<Customer>()
            .WithMany()
            .HasForeignKey(p => p.CustomerNumber)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
