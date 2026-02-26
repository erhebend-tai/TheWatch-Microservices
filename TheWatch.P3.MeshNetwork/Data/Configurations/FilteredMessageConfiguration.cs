using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TheWatch.P3.MeshNetwork.Mesh;

namespace TheWatch.P3.MeshNetwork.Data.Configurations;

public class FilteredMessageConfiguration : IEntityTypeConfiguration<FilteredMessage>
{
    public void Configure(EntityTypeBuilder<FilteredMessage> builder)
    {
        builder.Property(e => e.FilterReason).HasMaxLength(500).IsRequired();
        builder.Property(e => e.MatchedPattern).HasMaxLength(500);
        builder.Property(e => e.MatchedCategory).HasMaxLength(100);
        builder.Property(e => e.RedactedContent).HasMaxLength(500);
        builder.Property(e => e.ReviewNotes).HasMaxLength(500);
        builder.Property(e => e.AppealResult).HasMaxLength(30);
        builder.HasIndex(e => e.BroadcastId);
        builder.HasIndex(e => e.FilterId);
        builder.HasIndex(e => e.FilterAction);
        builder.HasIndex(e => e.MatchedCategory);
        builder.HasIndex(e => e.ReviewStatus);
        builder.HasIndex(e => e.FilteredAt).IsDescending();
        builder.ToTable(t => {
            t.HasCheckConstraint("CK_FilteredMessage_Confidence", "[ConfidenceScore] BETWEEN 0.0 AND 1.0");
        });
    }
}
