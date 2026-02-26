using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TheWatch.P4.Wearable.Devices;

namespace TheWatch.P4.Wearable.Data.Configurations;

public class WearableVoiceTriggerConfiguration : IEntityTypeConfiguration<WearableVoiceTrigger>
{
    public void Configure(EntityTypeBuilder<WearableVoiceTrigger> builder)
    {
        builder.Property(e => e.TriggerPhrase).HasMaxLength(500).IsRequired();
        builder.Property(e => e.DetectedPhrase).HasMaxLength(500);
        builder.Property(e => e.ActionTaken).HasMaxLength(200);

        builder.HasIndex(e => e.DeviceId);
        builder.HasIndex(e => e.UserId);
        builder.HasIndex(e => e.WasEmergency);
        builder.HasIndex(e => e.CreatedAt).IsDescending();

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_WearableVoiceTrigger_Confidence", "[ConfidenceScore] BETWEEN 0.0 AND 1.0");
        });
    }
}
