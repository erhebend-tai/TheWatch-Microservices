using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TheWatch.P3.MeshNetwork.Mesh;

namespace TheWatch.P3.MeshNetwork.Data.Configurations;

public class DeviceApprovalConfiguration : IEntityTypeConfiguration<DeviceApproval>
{
    public void Configure(EntityTypeBuilder<DeviceApproval> builder)
    {
        builder.Property(e => e.PermissionsGranted).HasColumnType("nvarchar(max)");
        builder.Property(e => e.RevocationReason).HasMaxLength(500);
        builder.Property(e => e.RequestMessage).HasMaxLength(500);
        builder.Property(e => e.ResponseMessage).HasMaxLength(500);
        builder.HasIndex(e => e.RequestingNodeId);
        builder.HasIndex(e => e.ApprovingNodeId);
        builder.HasIndex(e => e.ApprovalStatus);
        builder.HasIndex(e => new { e.ValidFrom, e.ValidUntil });
        builder.ToTable(t => {
            t.HasCheckConstraint("CK_DeviceApproval_Level", "[ApprovalLevel] BETWEEN 1 AND 5");
            t.HasCheckConstraint("CK_DeviceApproval_Trust", "[TrustScoreGranted] BETWEEN 0.0 AND 1.0");
        });
    }
}
