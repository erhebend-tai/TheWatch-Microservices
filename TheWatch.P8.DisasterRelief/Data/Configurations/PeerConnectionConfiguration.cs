using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TheWatch.P8.DisasterRelief.Relief;

namespace TheWatch.P8.DisasterRelief.Data.Configurations;

public class PeerConnectionConfiguration : IEntityTypeConfiguration<PeerConnection>
{
    public void Configure(EntityTypeBuilder<PeerConnection> builder)
    {
        builder.Property(e => e.SharedExperience).HasMaxLength(2000);
        builder.HasIndex(e => e.RequesterId);
        builder.HasIndex(e => e.ResponderId);
        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => e.DisasterEventId);
    }
}
