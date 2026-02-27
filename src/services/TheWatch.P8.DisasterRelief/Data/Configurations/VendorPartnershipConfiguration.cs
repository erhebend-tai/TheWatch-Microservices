using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TheWatch.P8.DisasterRelief.Relief;

namespace TheWatch.P8.DisasterRelief.Data.Configurations;

public class VendorPartnershipConfiguration : IEntityTypeConfiguration<VendorPartnership>
{
    public void Configure(EntityTypeBuilder<VendorPartnership> builder)
    {
        builder.Property(e => e.VendorName).HasMaxLength(200).IsRequired();
        builder.Property(e => e.ContactName).HasMaxLength(200);
        builder.Property(e => e.ContactPhone).HasMaxLength(50);
        builder.Property(e => e.ContactEmail).HasMaxLength(256);
        builder.Property(e => e.Website).HasMaxLength(500);
        builder.Property(e => e.DiscountPct).HasPrecision(5, 2);
        builder.Property(e => e.TermsDescription).HasMaxLength(4000);
        builder.HasIndex(e => e.DisasterEventId);
        builder.HasIndex(e => e.Status);
        builder.ToTable(t => t.HasCheckConstraint("CK_VendorPartnership_Discount", "[DiscountPct] IS NULL OR ([DiscountPct] BETWEEN 0 AND 100)"));
    }
}
