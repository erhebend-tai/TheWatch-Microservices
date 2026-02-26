using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TheWatch.P8.DisasterRelief.Relief;

namespace TheWatch.P8.DisasterRelief.Data.Configurations;

public class EvacuationRouteConfiguration : IEntityTypeConfiguration<EvacuationRoute>
{
    public void Configure(EntityTypeBuilder<EvacuationRoute> builder)
    {
        builder.Property(e => e.Description).HasMaxLength(4000);
        builder.HasIndex(e => e.DisasterEventId);
        builder.HasIndex(e => e.IsActive);
        builder.ToTable(t => {
            t.HasCheckConstraint("CK_EvacuationRoute_Distance", "[DistanceKm] > 0");
            t.HasCheckConstraint("CK_EvacuationRoute_Time", "[EstimatedTimeMinutes] > 0");
        });
    }
}
