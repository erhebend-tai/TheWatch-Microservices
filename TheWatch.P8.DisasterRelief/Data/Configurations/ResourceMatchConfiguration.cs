using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TheWatch.P8.DisasterRelief.Relief;

namespace TheWatch.P8.DisasterRelief.Data.Configurations;

public class ResourceMatchConfiguration : IEntityTypeConfiguration<ResourceMatch>
{
    public void Configure(EntityTypeBuilder<ResourceMatch> builder)
    {
        builder.Property(e => e.Notes).HasMaxLength(1000);
        builder.HasIndex(e => e.ResourceId);
        builder.HasIndex(e => e.RequestId);
        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => e.VolunteerId);
        builder.ToTable(t => t.HasCheckConstraint("CK_ResourceMatch_Quantity", "[QuantityMatched] > 0"));
    }
}
