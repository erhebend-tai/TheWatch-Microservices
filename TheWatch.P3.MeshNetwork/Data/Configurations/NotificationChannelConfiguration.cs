using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TheWatch.P3.MeshNetwork.Mesh;

namespace TheWatch.P3.MeshNetwork.Data.Configurations;

public class NotificationChannelConfiguration : IEntityTypeConfiguration<NotificationChannel>
{
    public void Configure(EntityTypeBuilder<NotificationChannel> builder)
    {
        builder.Property(e => e.Name).HasMaxLength(200).IsRequired();
        builder.HasIndex(e => e.Type);
    }
}
