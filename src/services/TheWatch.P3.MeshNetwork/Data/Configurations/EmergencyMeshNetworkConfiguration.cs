using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TheWatch.P3.MeshNetwork.Mesh;

namespace TheWatch.P3.MeshNetwork.Data.Configurations;

public class EmergencyMeshNetworkConfiguration : IEntityTypeConfiguration<EmergencyMeshNetwork>
{
    public void Configure(EntityTypeBuilder<EmergencyMeshNetwork> builder)
    {
        builder.Property(e => e.NetworkName).HasMaxLength(200).IsRequired();
        builder.Property(e => e.NetworkCode).HasMaxLength(50).IsRequired();
        builder.Property(e => e.EmergencyType).HasMaxLength(100).IsRequired();
        builder.Property(e => e.FrequencyBand).HasMaxLength(50);
        builder.Property(e => e.ProtocolVersion).HasMaxLength(20);
        builder.HasIndex(e => e.NetworkCode).IsUnique();
        builder.HasIndex(e => e.ActivationStatus);
        builder.HasIndex(e => e.EmergencyType);
        builder.HasIndex(e => e.SeverityLevel);
        builder.HasIndex(e => new { e.CenterLatitude, e.CenterLongitude });
        builder.ToTable(t => {
            t.HasCheckConstraint("CK_EmergencyMeshNetwork_Severity", "[SeverityLevel] BETWEEN 1 AND 5");
        });
    }
}
