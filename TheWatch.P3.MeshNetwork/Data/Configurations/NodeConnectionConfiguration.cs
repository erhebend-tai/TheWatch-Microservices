using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TheWatch.P3.MeshNetwork.Mesh;

namespace TheWatch.P3.MeshNetwork.Data.Configurations;

public class NodeConnectionConfiguration : IEntityTypeConfiguration<NodeConnection>
{
    public void Configure(EntityTypeBuilder<NodeConnection> builder)
    {
        builder.Property(e => e.ConnectionType).HasMaxLength(50).IsRequired();
        builder.HasIndex(e => e.NodeAId);
        builder.HasIndex(e => e.NodeBId);
        builder.HasIndex(e => e.IsActive);
        builder.HasIndex(e => e.EstablishedAt).IsDescending();
    }
}
