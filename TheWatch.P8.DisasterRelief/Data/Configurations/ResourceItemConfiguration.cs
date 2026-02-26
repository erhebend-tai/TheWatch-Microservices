using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TheWatch.P8.DisasterRelief.Relief;

namespace TheWatch.P8.DisasterRelief.Data.Configurations;

public class ResourceItemConfiguration : IEntityTypeConfiguration<ResourceItem>
{
    public void Configure(EntityTypeBuilder<ResourceItem> builder)
    {
        builder.Property(e => e.Name).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Unit).HasMaxLength(50);
        builder.HasIndex(e => e.Category);
        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => e.DisasterEventId);
        builder.ToTable(t => t.HasCheckConstraint("CK_ResourceItem_Quantity", "[Quantity] > 0"));
    }
}
