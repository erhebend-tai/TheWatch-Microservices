using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TheWatch.P3.MeshNetwork.Mesh;

namespace TheWatch.P3.MeshNetwork.Data.Configurations;

public class PhraseRelayConfiguration : IEntityTypeConfiguration<PhraseRelay>
{
    public void Configure(EntityTypeBuilder<PhraseRelay> builder)
    {
        builder.Property(e => e.ProtocolUsed).HasMaxLength(50);
        builder.Property(e => e.FailureReason).HasMaxLength(500);
        builder.HasIndex(e => e.BroadcastId);
        builder.HasIndex(e => e.RelayNodeId);
        builder.HasIndex(e => e.SourceNodeId);
        builder.HasIndex(e => e.RelayStatus);
        builder.HasIndex(e => e.HopNumber);
        builder.HasIndex(e => e.RelayedAt).IsDescending();
    }
}
