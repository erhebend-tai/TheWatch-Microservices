using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TheWatch.P4.Wearable.Devices;

namespace TheWatch.P4.Wearable.Data.Configurations;

public class WearableDeviceConfiguration : IEntityTypeConfiguration<WearableDevice>
{
    public void Configure(EntityTypeBuilder<WearableDevice> builder)
    {
        builder.Property(e => e.Name).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Model).HasMaxLength(200);
        builder.Property(e => e.FirmwareVersion).HasMaxLength(50);
        builder.Property(e => e.ScreenShape).HasMaxLength(50);
        builder.Property(e => e.BluetoothVersion).HasMaxLength(20);

        builder.HasIndex(e => e.OwnerId);
        builder.HasIndex(e => e.Platform);
        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => e.LastSyncAt).IsDescending();

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_WearableDevice_Battery", "[BatteryPercent] IS NULL OR ([BatteryPercent] BETWEEN 0 AND 100)");
            t.HasCheckConstraint("CK_WearableDevice_BatteryHealth", "[BatteryHealthPct] IS NULL OR ([BatteryHealthPct] BETWEEN 0.0 AND 100.0)");
        });
    }
}
