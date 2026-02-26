using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TheWatch.P4.Wearable.Devices;

namespace TheWatch.P4.Wearable.Data.Configurations;

public class WearableAlertConfiguration : IEntityTypeConfiguration<WearableAlert>
{
    public void Configure(EntityTypeBuilder<WearableAlert> builder)
    {
        builder.Property(e => e.Message).HasMaxLength(500).IsRequired();
        builder.Property(e => e.ResolutionNotes).HasMaxLength(2000);

        builder.HasIndex(e => e.DeviceId);
        builder.HasIndex(e => e.UserId);
        builder.HasIndex(e => new { e.AlertType, e.Status });
        builder.HasIndex(e => new { e.Severity, e.CreatedAt }).IsDescending(false, true);
    }
}
