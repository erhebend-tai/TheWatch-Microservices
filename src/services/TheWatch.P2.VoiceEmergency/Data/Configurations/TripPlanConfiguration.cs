using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TheWatch.P2.VoiceEmergency.Emergency;

namespace TheWatch.P2.VoiceEmergency.Data.Configurations;

public class TripPlanConfiguration : IEntityTypeConfiguration<TripPlan>
{
    public void Configure(EntityTypeBuilder<TripPlan> builder)
    {
        builder.Property(e => e.TripName).HasMaxLength(200).IsRequired();
        builder.Property(e => e.DepartureLocation).HasMaxLength(500).IsRequired();
        builder.Property(e => e.DepartureLatitude).HasPrecision(10, 7);
        builder.Property(e => e.DepartureLongitude).HasPrecision(10, 7);
        builder.Property(e => e.DestinationLocation).HasMaxLength(500).IsRequired();
        builder.Property(e => e.DestinationLatitude).HasPrecision(10, 7);
        builder.Property(e => e.DestinationLongitude).HasPrecision(10, 7);
        builder.Property(e => e.WaypointsJson).HasColumnType("nvarchar(max)");
        builder.Property(e => e.EmergencyContactIds).HasColumnType("nvarchar(max)");
        builder.Property(e => e.VehicleDescription).HasMaxLength(500);
        builder.Property(e => e.EquipmentNotes).HasColumnType("nvarchar(max)");
        builder.Property(e => e.RouteDescription).HasColumnType("nvarchar(max)");
        builder.Property(e => e.HazardNotes).HasColumnType("nvarchar(max)");
        builder.Property(e => e.SatelliteDeviceId).HasMaxLength(200);

        builder.HasIndex(e => new { e.UserId, e.TripStatus, e.IsDeleted });
        builder.HasIndex(e => new { e.TripStatus, e.PlannedArrivalAt });
        builder.HasIndex(e => e.NextCheckinDueAt);

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_TripPlan_Dates", "[PlannedArrivalAt] > [PlannedDepartureAt]");
            t.HasCheckConstraint("CK_TripPlan_Overdue", "[OverdueThresholdMin] > 0");
            t.HasCheckConstraint("CK_TripPlan_MissedCheckins", "[MissedCheckinsCount] >= 0");
        });
    }
}
