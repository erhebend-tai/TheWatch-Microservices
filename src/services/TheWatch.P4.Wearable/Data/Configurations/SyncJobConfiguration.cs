using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TheWatch.P4.Wearable.Devices;

namespace TheWatch.P4.Wearable.Data.Configurations;

public class SyncJobConfiguration : IEntityTypeConfiguration<SyncJob>
{
    public void Configure(EntityTypeBuilder<SyncJob> builder)
    {
        builder.Property(e => e.ErrorMessage).HasMaxLength(500);
        builder.HasIndex(e => e.DeviceId);
        builder.HasIndex(e => e.StartedAt).IsDescending();
        builder.HasIndex(e => new { e.DeviceId, e.Success });
    }
}
