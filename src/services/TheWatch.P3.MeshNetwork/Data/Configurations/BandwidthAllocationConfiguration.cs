using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TheWatch.P3.MeshNetwork.Mesh;

namespace TheWatch.P3.MeshNetwork.Data.Configurations;

public class BandwidthAllocationConfiguration : IEntityTypeConfiguration<BandwidthAllocation>
{
    public void Configure(EntityTypeBuilder<BandwidthAllocation> builder)
    {
        builder.Property(e => e.AllocationType).HasMaxLength(50);
        builder.HasIndex(e => e.NodeId);
        builder.HasIndex(e => e.ChannelId);
        builder.HasIndex(e => e.NetworkId);
        builder.HasIndex(e => e.IsThrottled);
        builder.ToTable(t => {
            t.HasCheckConstraint("CK_BandwidthAllocation_Allocated", "[AllocatedKbps] >= 0");
            t.HasCheckConstraint("CK_BandwidthAllocation_Used", "[UsedKbps] >= 0");
            t.HasCheckConstraint("CK_BandwidthAllocation_Priority", "[PriorityLevel] BETWEEN 1 AND 5");
        });
    }
}
