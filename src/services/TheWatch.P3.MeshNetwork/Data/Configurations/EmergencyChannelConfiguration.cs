using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TheWatch.P3.MeshNetwork.Mesh;

namespace TheWatch.P3.MeshNetwork.Data.Configurations;

public class EmergencyChannelConfiguration : IEntityTypeConfiguration<EmergencyChannel>
{
    public void Configure(EntityTypeBuilder<EmergencyChannel> builder)
    {
        builder.Property(e => e.ChannelCode).HasMaxLength(50).IsRequired();
        builder.Property(e => e.ChannelName).HasMaxLength(200).IsRequired();
        builder.Property(e => e.ChannelType).HasMaxLength(50);
        builder.Property(e => e.FrequencyBand).HasMaxLength(50);
        builder.HasIndex(e => e.ChannelCode).IsUnique();
        builder.HasIndex(e => e.ChannelType);
        builder.HasIndex(e => e.NetworkId);
        builder.HasIndex(e => e.ChannelStatus);
        builder.ToTable(t => {
            t.HasCheckConstraint("CK_EmergencyChannel_Priority", "[PriorityFloor] BETWEEN 1 AND 5");
            t.HasCheckConstraint("CK_EmergencyChannel_Trust", "[MinTrustScore] BETWEEN 0.0 AND 1.0");
            t.HasCheckConstraint("CK_EmergencyChannel_Participants", "[CurrentParticipants] <= [MaxParticipants]");
        });
    }
}
