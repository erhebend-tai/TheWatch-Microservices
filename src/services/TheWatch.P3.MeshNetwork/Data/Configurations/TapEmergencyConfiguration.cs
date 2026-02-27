using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TheWatch.P3.MeshNetwork.Mesh;

namespace TheWatch.P3.MeshNetwork.Data.Configurations;

public class TapEmergencyConfiguration : IEntityTypeConfiguration<TapEmergency>
{
    public void Configure(EntityTypeBuilder<TapEmergency> builder)
    {
        builder.Property(e => e.DetectedPattern).HasMaxLength(200).IsRequired();
        builder.Property(e => e.EmergencyType).HasMaxLength(100).IsRequired();
        builder.Property(e => e.CancelReason).HasMaxLength(200);
        builder.HasIndex(e => e.NodeId);
        builder.HasIndex(e => e.EmergencyType);
        builder.HasIndex(e => e.EmergencySeverity);
        builder.HasIndex(e => e.ResponseStatus);
        builder.HasIndex(e => e.DetectedAt).IsDescending();
        builder.HasIndex(e => new { e.Latitude, e.Longitude });
        builder.ToTable(t => {
            t.HasCheckConstraint("CK_TapEmergency_Confidence", "[PatternConfidence] BETWEEN 0.0 AND 1.0");
            t.HasCheckConstraint("CK_TapEmergency_Severity", "[EmergencySeverity] BETWEEN 1 AND 5");
        });
    }
}
