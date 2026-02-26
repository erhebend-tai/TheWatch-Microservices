using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TheWatch.P3.MeshNetwork.Mesh;

namespace TheWatch.P3.MeshNetwork.Data.Configurations;

public class MeshNodeConfiguration : IEntityTypeConfiguration<MeshNode>
{
    public void Configure(EntityTypeBuilder<MeshNode> builder)
    {
        builder.Property(e => e.Name).HasMaxLength(200);
        builder.Property(e => e.DeviceId).HasMaxLength(128);
        builder.HasIndex(e => e.DeviceId);
        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => e.LastSeenAt).IsDescending();
    }
}
