using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TheWatch.P4.Wearable.Devices;

namespace TheWatch.P4.Wearable.Data.Configurations;

public class HeartbeatReadingConfiguration : IEntityTypeConfiguration<HeartbeatReading>
{
    public void Configure(EntityTypeBuilder<HeartbeatReading> builder)
    {
        builder.HasIndex(e => e.DeviceId);
        builder.HasIndex(e => e.RecordedAt).IsDescending();
        builder.HasIndex(e => new { e.DeviceId, e.RecordedAt }).IsDescending(false, true);

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_HeartbeatReading_Bpm", "[Bpm] BETWEEN 20 AND 300");
        });
    }
}
