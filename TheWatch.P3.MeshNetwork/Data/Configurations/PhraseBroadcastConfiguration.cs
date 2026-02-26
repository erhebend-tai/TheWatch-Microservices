using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TheWatch.P3.MeshNetwork.Mesh;

namespace TheWatch.P3.MeshNetwork.Data.Configurations;

public class PhraseBroadcastConfiguration : IEntityTypeConfiguration<PhraseBroadcast>
{
    public void Configure(EntityTypeBuilder<PhraseBroadcast> builder)
    {
        builder.Property(e => e.PhraseText).HasMaxLength(500).IsRequired();
        builder.Property(e => e.ErrorMessage).HasMaxLength(500);
        builder.HasIndex(e => e.SenderNodeId);
        builder.HasIndex(e => e.BroadcastStatus);
        builder.HasIndex(e => e.PriorityLevel);
        builder.HasIndex(e => e.CreatedAt).IsDescending();
        builder.HasIndex(e => new { e.Latitude, e.Longitude });
        builder.HasIndex(e => e.ExpiresAt);
        builder.ToTable(t => {
            t.HasCheckConstraint("CK_PhraseBroadcast_Priority", "[PriorityLevel] BETWEEN 1 AND 5");
            t.HasCheckConstraint("CK_PhraseBroadcast_Hops", "[HopCount] <= [MaxHops]");
            t.HasCheckConstraint("CK_PhraseBroadcast_Radius", "[BroadcastRadiusMeters] > 0");
        });
    }
}
