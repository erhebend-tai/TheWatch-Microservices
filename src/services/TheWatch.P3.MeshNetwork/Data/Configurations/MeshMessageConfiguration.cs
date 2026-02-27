using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TheWatch.P3.MeshNetwork.Mesh;

namespace TheWatch.P3.MeshNetwork.Data.Configurations;

public class MeshMessageConfiguration : IEntityTypeConfiguration<MeshMessage>
{
    public void Configure(EntityTypeBuilder<MeshMessage> builder)
    {
        builder.Property(e => e.Content).HasMaxLength(4000);
        builder.HasIndex(e => e.SenderId);
        builder.HasIndex(e => e.RecipientId);
        builder.HasIndex(e => e.ChannelId);
        builder.HasIndex(e => new { e.Priority, e.SentAt }).IsDescending(false, true);
    }
}
