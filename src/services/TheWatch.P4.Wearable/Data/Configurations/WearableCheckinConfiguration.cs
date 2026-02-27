using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TheWatch.P4.Wearable.Devices;

namespace TheWatch.P4.Wearable.Data.Configurations;

public class WearableCheckinConfiguration : IEntityTypeConfiguration<WearableCheckin>
{
    public void Configure(EntityTypeBuilder<WearableCheckin> builder)
    {
        builder.Property(e => e.Notes).HasMaxLength(500);

        builder.HasIndex(e => e.DeviceId);
        builder.HasIndex(e => e.UserId);
        builder.HasIndex(e => e.CreatedAt).IsDescending();
        builder.HasIndex(e => e.IsEmergency);

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_WearableCheckin_Battery", "[BatteryPercent] IS NULL OR ([BatteryPercent] BETWEEN 0 AND 100)");
        });
    }
}
