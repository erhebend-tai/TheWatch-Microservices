using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TheWatch.P2.VoiceEmergency.Emergency;

namespace TheWatch.P2.VoiceEmergency.Data.Configurations;

public class WelfareCheckConfiguration : IEntityTypeConfiguration<WelfareCheck>
{
    public void Configure(EntityTypeBuilder<WelfareCheck> builder)
    {
        builder.Property(e => e.ResponseText).HasMaxLength(500);
        builder.Property(e => e.VerificationMethod).HasMaxLength(50);
        builder.Property(e => e.EscalatedTo).HasColumnType("nvarchar(max)");
        builder.Property(e => e.Notes).HasColumnType("nvarchar(max)");

        builder.HasIndex(e => new { e.UserId, e.CheckStatus, e.IsDeleted });
        builder.HasIndex(e => new { e.CheckStatus, e.NextAttemptAt });
        builder.HasIndex(e => e.AlertId);
        builder.HasIndex(e => e.TripId);

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_WelfareCheck_Attempts", "[AttemptNumber] >= 1 AND [AttemptNumber] <= [MaxAttempts]");
        });
    }
}
