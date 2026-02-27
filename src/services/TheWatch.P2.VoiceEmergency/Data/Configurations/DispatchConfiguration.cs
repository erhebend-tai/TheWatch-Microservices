using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TheWatch.P2.VoiceEmergency.Emergency;

namespace TheWatch.P2.VoiceEmergency.Data.Configurations;

public class DispatchConfiguration : IEntityTypeConfiguration<Dispatch>
{
    public void Configure(EntityTypeBuilder<Dispatch> builder)
    {
        builder.HasIndex(e => new { e.IncidentId, e.Status });
        builder.HasIndex(e => new { e.Status, e.CreatedAt }).IsDescending(false, true);

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_Dispatch_Radius", "[RadiusKm] > 0");
            t.HasCheckConstraint("CK_Dispatch_Responders", "[RespondersRequested] >= 1");
        });
    }
}
