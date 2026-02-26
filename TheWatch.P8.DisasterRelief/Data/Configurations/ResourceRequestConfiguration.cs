using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TheWatch.P8.DisasterRelief.Relief;

namespace TheWatch.P8.DisasterRelief.Data.Configurations;

public class ResourceRequestConfiguration : IEntityTypeConfiguration<ResourceRequest>
{
    public void Configure(EntityTypeBuilder<ResourceRequest> builder)
    {
        builder.HasIndex(e => e.RequesterId);
        builder.HasIndex(e => new { e.Category, e.Status });
        builder.HasIndex(e => new { e.Priority, e.CreatedAt }).IsDescending(false, true);
        builder.HasIndex(e => e.DisasterEventId);
        builder.ToTable(t => t.HasCheckConstraint("CK_ResourceRequest_Quantity", "[Quantity] > 0"));
    }
}
