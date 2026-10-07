using GenApp.Domain.Policies;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GenApp.Infrastructure.Persistence.Configurations;

public class CommercialPolicyConfiguration : IEntityTypeConfiguration<CommercialPolicy>
{
    public void Configure(EntityTypeBuilder<CommercialPolicy> b)
    {
        b.ToTable("commercial");
        b.HasKey(c => c.PolicyNumber);
        b.Property(c => c.PolicyNumber).ValueGeneratedNever();

        b.Property(c => c.RequestDate).HasColumnType("timestamp without time zone");
        b.Property(c => c.Address).HasMaxLength(255);
        b.Property(c => c.Zipcode).HasMaxLength(8);
        b.Property(c => c.LatitudeN).HasMaxLength(11);
        b.Property(c => c.LongitudeW).HasMaxLength(11);
        b.Property(c => c.Customer).HasMaxLength(255);
        b.Property(c => c.PropertyType).HasMaxLength(255);
        b.Property(c => c.RejectionReason).HasMaxLength(255);

        b.HasOne<Policy>()
            .WithOne()
            .HasForeignKey<CommercialPolicy>(c => c.PolicyNumber)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
