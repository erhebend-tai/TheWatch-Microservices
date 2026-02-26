using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TheWatch.P2.VoiceEmergency.Emergency;

namespace TheWatch.P2.VoiceEmergency.Data.Configurations;

public class VoiceTriggerConfiguration : IEntityTypeConfiguration<VoiceTrigger>
{
    public void Configure(EntityTypeBuilder<VoiceTrigger> builder)
    {
        builder.Property(e => e.TriggerPhrase).HasMaxLength(500).IsRequired();
        builder.Property(e => e.LanguageCode).HasMaxLength(10).IsRequired();
        builder.Property(e => e.ConfidenceThreshold).HasPrecision(5, 4);
        builder.Property(e => e.CustomActionPayload).HasColumnType("nvarchar(max)");

        builder.HasIndex(e => new { e.UserId, e.IsActive, e.IsDeleted });
        builder.HasIndex(e => new { e.TriggerType, e.IsActive });
        builder.HasIndex(e => e.LastTriggeredAt).IsDescending();

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_VoiceTrigger_Confidence", "[ConfidenceThreshold] BETWEEN 0.0000 AND 1.0000");
            t.HasCheckConstraint("CK_VoiceTrigger_Priority", "[PriorityLevel] BETWEEN 1 AND 10");
            t.HasCheckConstraint("CK_VoiceTrigger_Cooldown", "[CooldownSeconds] >= 0");
        });
    }
}
