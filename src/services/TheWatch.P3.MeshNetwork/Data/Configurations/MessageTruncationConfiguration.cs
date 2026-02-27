using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TheWatch.P3.MeshNetwork.Mesh;

namespace TheWatch.P3.MeshNetwork.Data.Configurations;

public class MessageTruncationConfiguration : IEntityTypeConfiguration<MessageTruncation>
{
    public void Configure(EntityTypeBuilder<MessageTruncation> builder)
    {
        builder.Property(e => e.TruncationReason).HasMaxLength(200).IsRequired();
        builder.Property(e => e.OriginalContentRef).HasMaxLength(500);
        builder.Property(e => e.KeyPhrasesPreserved).HasColumnType("nvarchar(max)");
        builder.HasIndex(e => e.BroadcastId);
        builder.HasIndex(e => e.TruncationMethod);
        builder.ToTable(t => {
            t.HasCheckConstraint("CK_MessageTruncation_Preserved", "[ContentPreservedPct] BETWEEN 0.0 AND 100.0");
            t.HasCheckConstraint("CK_MessageTruncation_Length", "[TruncatedLengthBytes] <= [OriginalLengthBytes]");
        });
    }
}
