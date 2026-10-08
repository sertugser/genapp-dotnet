using GenApp.Domain.Policies;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GenApp.Infrastructure.Persistence.Configurations;

public class MotorPolicyConfiguration : IEntityTypeConfiguration<MotorPolicy>
{
    public void Configure(EntityTypeBuilder<MotorPolicy> b)
    {
        b.ToTable("motor");
        b.HasKey(m => m.PolicyNumber);
        b.Property(m => m.PolicyNumber).ValueGeneratedNever();

        b.Property(m => m.Make).HasMaxLength(15);
        b.Property(m => m.Model).HasMaxLength(15);
        b.Property(m => m.RegNumber).HasMaxLength(7);
        b.Property(m => m.Colour).HasMaxLength(8);

        b.HasOne<Policy>()
            .WithOne()
            .HasForeignKey<MotorPolicy>(m => m.PolicyNumber)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
