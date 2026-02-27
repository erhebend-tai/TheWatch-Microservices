using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TheWatch.P1.CoreGateway.Measurements;

namespace TheWatch.P1.CoreGateway.Data.Configurations;

public class MeasurementRangeConfiguration : IEntityTypeConfiguration<MeasurementRange>
{
    public void Configure(EntityTypeBuilder<MeasurementRange> builder)
    {
        builder.Property(e => e.Name).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Context).HasMaxLength(200);
        builder.HasIndex(e => e.UnitId);
        builder.ToTable(t => t.HasCheckConstraint("CK_MeasurementRange_MinMax", "[MinValue] < [MaxValue]"));
    }
}
