using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TheWatch.P3.MeshNetwork.Mesh;

namespace TheWatch.P3.MeshNetwork.Data.Configurations;

public class MeshAuditLogConfiguration : IEntityTypeConfiguration<MeshAuditLog>
{
    public void Configure(EntityTypeBuilder<MeshAuditLog> builder)
    {
        builder.Property(e => e.Action).HasMaxLength(100).IsRequired();
        builder.Property(e => e.EntityType).HasMaxLength(100).IsRequired();
        builder.Property(e => e.OldValue).HasColumnType("nvarchar(max)");
        builder.Property(e => e.NewValue).HasColumnType("nvarchar(max)");
        builder.Property(e => e.IpAddress).HasMaxLength(50);
        builder.Property(e => e.Details).HasColumnType("nvarchar(max)");
        builder.HasIndex(e => e.NodeId);
        builder.HasIndex(e => e.UserId);
        builder.HasIndex(e => new { e.EntityType, e.EntityId });
        builder.HasIndex(e => e.PerformedAt).IsDescending();
    }
}
