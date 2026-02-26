using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TheWatch.P3.MeshNetwork.Mesh;

namespace TheWatch.P3.MeshNetwork.Data.Configurations;

public class ContentFilterConfiguration : IEntityTypeConfiguration<ContentFilter>
{
    public void Configure(EntityTypeBuilder<ContentFilter> builder)
    {
        builder.Property(e => e.FilterName).HasMaxLength(200).IsRequired();
        builder.Property(e => e.FilterCategory).HasMaxLength(100).IsRequired();
        builder.Property(e => e.PatternValue).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(e => e.ReplacementValue).HasMaxLength(500);
        builder.HasIndex(e => e.FilterType);
        builder.HasIndex(e => e.FilterCategory);
        builder.HasIndex(e => e.ActionOnMatch);
        builder.HasIndex(e => e.IsEnabled);
        builder.HasIndex(e => e.PriorityOrder);
        builder.ToTable(t => {
            t.HasCheckConstraint("CK_ContentFilter_Confidence", "[ConfidenceThreshold] BETWEEN 0.0 AND 1.0");
            t.HasCheckConstraint("CK_ContentFilter_Severity", "[SeverityLevel] BETWEEN 1 AND 5");
        });
    }
}
