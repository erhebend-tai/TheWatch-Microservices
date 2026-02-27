using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TheWatch.P3.MeshNetwork.Mesh;

namespace TheWatch.P3.MeshNetwork.Data.Configurations;

public class AllowedMessageTypeConfiguration : IEntityTypeConfiguration<AllowedMessageType>
{
    public void Configure(EntityTypeBuilder<AllowedMessageType> builder)
    {
        builder.Property(e => e.TypeCode).HasMaxLength(50).IsRequired();
        builder.Property(e => e.TypeName).HasMaxLength(200).IsRequired();
        builder.Property(e => e.TypeCategory).HasMaxLength(100).IsRequired();
        builder.Property(e => e.Description).HasMaxLength(500);
        builder.HasIndex(e => e.TypeCode).IsUnique();
        builder.HasIndex(e => e.TypeCategory);
        builder.HasIndex(e => e.IsEnabled);
        builder.ToTable(t => {
            t.HasCheckConstraint("CK_AllowedMessageType_Trust", "[MinTrustScore] BETWEEN 0.0 AND 1.0");
            t.HasCheckConstraint("CK_AllowedMessageType_Priority", "[PriorityDefault] BETWEEN 1 AND 5");
        });
    }
}
