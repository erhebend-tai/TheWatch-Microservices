using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TheWatch.P8.DisasterRelief.Relief;

namespace TheWatch.P8.DisasterRelief.Data.Configurations;

public class DirectMessageConfiguration : IEntityTypeConfiguration<DirectMessage>
{
    public void Configure(EntityTypeBuilder<DirectMessage> builder)
    {
        builder.Property(e => e.Content).HasMaxLength(4000).IsRequired();
        builder.HasIndex(e => e.SenderId);
        builder.HasIndex(e => e.RecipientId);
        builder.HasIndex(e => e.ConnectionId);
        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => e.CreatedAt).IsDescending();
    }
}
