using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TheWatch.P8.DisasterRelief.Relief;

namespace TheWatch.P8.DisasterRelief.Data.Configurations;

public class ResourceNeedConfiguration : IEntityTypeConfiguration<ResourceNeed>
{
    public void Configure(EntityTypeBuilder<ResourceNeed> builder)
    {
        builder.Property(e => e.Description).HasMaxLength(1000).IsRequired();
        builder.Property(e => e.Unit).HasMaxLength(50);
        builder.HasIndex(e => e.VictimId);
        builder.HasIndex(e => e.DisasterEventId);
        builder.HasIndex(e => new { e.Category, e.Urgency });
        builder.HasIndex(e => e.Status);
        builder.ToTable(t => t.HasCheckConstraint("CK_ResourceNeed_Quantity", "[Quantity] >= 1"));
    }
}
