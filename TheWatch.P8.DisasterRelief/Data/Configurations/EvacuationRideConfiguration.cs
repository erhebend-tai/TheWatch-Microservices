using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TheWatch.P8.DisasterRelief.Relief;

namespace TheWatch.P8.DisasterRelief.Data.Configurations;

public class EvacuationRideConfiguration : IEntityTypeConfiguration<EvacuationRide>
{
    public void Configure(EntityTypeBuilder<EvacuationRide> builder)
    {
        builder.Property(e => e.VehicleDescription).HasMaxLength(500).IsRequired();
        builder.Property(e => e.Notes).HasMaxLength(1000);
        builder.HasIndex(e => e.DisasterEventId);
        builder.HasIndex(e => e.DriverId);
        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => e.DepartureTime);
        builder.ToTable(t => {
            t.HasCheckConstraint("CK_EvacuationRide_Seats", "[AvailableSeats] > 0");
            t.HasCheckConstraint("CK_EvacuationRide_Claimed", "[ClaimedSeats] >= 0 AND [ClaimedSeats] <= [AvailableSeats]");
        });
    }
}
