using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TheWatch.P2.VoiceEmergency.Emergency;

namespace TheWatch.P2.VoiceEmergency.Data.Configurations;

public class IncidentConfiguration : IEntityTypeConfiguration<Incident>
{
    public void Configure(EntityTypeBuilder<Incident> builder)
    {
        builder.Property(e => e.Description).HasMaxLength(4000).IsRequired();
        builder.Property(e => e.ReporterName).HasMaxLength(200);
        builder.Property(e => e.ReporterPhone).HasMaxLength(50);

        builder.HasIndex(e => new { e.Status, e.CreatedAt }).IsDescending(false, true);
        builder.HasIndex(e => new { e.Type, e.Status });
        builder.HasIndex(e => e.ReporterId);

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_Incident_Severity", "[Severity] BETWEEN 1 AND 5");
        });
    }
}
