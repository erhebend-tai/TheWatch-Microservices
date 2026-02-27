using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TheWatch.P2.VoiceEmergency.Emergency;

namespace TheWatch.P2.VoiceEmergency.Data.Configurations;

public class VoiceAlertConfiguration : IEntityTypeConfiguration<VoiceAlert>
{
    public void Configure(EntityTypeBuilder<VoiceAlert> builder)
    {
        builder.Property(e => e.DetectedPhrase).HasMaxLength(500);
        builder.Property(e => e.SpeechConfidence).HasPrecision(5, 4);
        builder.Property(e => e.AudioClipRef).HasMaxLength(500);
        builder.Property(e => e.LocationLatitude).HasPrecision(10, 7);
        builder.Property(e => e.LocationLongitude).HasPrecision(10, 7);
        builder.Property(e => e.LocationAccuracyM).HasPrecision(8, 2);
        builder.Property(e => e.LocationAddress).HasMaxLength(500);
        builder.Property(e => e.ResolutionNotes).HasColumnType("nvarchar(max)");

        builder.HasIndex(e => new { e.UserId, e.AlertStatus, e.IsDeleted });
        builder.HasIndex(e => new { e.AlertStatus, e.SeverityLevel, e.InitiatedAt }).IsDescending(false, false, true);
        builder.HasIndex(e => e.TriggerId);
        builder.HasIndex(e => new { e.ResponderId, e.AlertStatus });

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_VoiceAlert_Escalation", "[EscalationLevel] >= 0 AND [EscalationLevel] <= [MaxEscalationLevel]");
        });
    }
}
