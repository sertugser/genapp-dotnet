using GenApp.Domain.Policies;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GenApp.Infrastructure.Persistence.Configurations;

public class EndowmentPolicyConfiguration : IEntityTypeConfiguration<EndowmentPolicy>
{
    public void Configure(EntityTypeBuilder<EndowmentPolicy> b)
    {
        b.ToTable("endowment");
        b.HasKey(e => e.PolicyNumber);
        b.Property(e => e.PolicyNumber).ValueGeneratedNever();

        b.Property(e => e.Equities).HasMaxLength(1);
        b.Property(e => e.WithProfits).HasMaxLength(1);
        b.Property(e => e.ManagedFund).HasMaxLength(1);
        b.Property(e => e.FundName).HasMaxLength(10);
        b.Property(e => e.LifeAssured).HasMaxLength(31);
        b.Property(e => e.PaddingData).HasMaxLength(32606);

        b.HasOne<Policy>()
            .WithOne()
            .HasForeignKey<EndowmentPolicy>(e => e.PolicyNumber)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
