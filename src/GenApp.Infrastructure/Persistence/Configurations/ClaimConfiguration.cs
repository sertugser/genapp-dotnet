using GenApp.Domain.Claims;
using GenApp.Domain.Policies;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GenApp.Infrastructure.Persistence.Configurations;

public class ClaimConfiguration : IEntityTypeConfiguration<Claim>
{
    public void Configure(EntityTypeBuilder<Claim> b)
    {
        b.ToTable("claim");
        b.HasKey(c => c.ClaimNumber);

        b.Property(c => c.ClaimNumber)
            .UseIdentityByDefaultColumn()
            .HasIdentityOptions(startValue: 1000001);

        b.Property(c => c.Cause).HasMaxLength(255);
        b.Property(c => c.Observations).HasMaxLength(255);

        b.HasOne<Policy>()
            .WithMany()
            .HasForeignKey(c => c.PolicyNumber)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
