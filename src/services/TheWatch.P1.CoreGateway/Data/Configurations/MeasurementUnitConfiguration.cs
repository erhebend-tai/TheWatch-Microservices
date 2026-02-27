using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TheWatch.P1.CoreGateway.Measurements;

namespace TheWatch.P1.CoreGateway.Data.Configurations;

public class MeasurementUnitConfiguration : IEntityTypeConfiguration<MeasurementUnit>
{
    public void Configure(EntityTypeBuilder<MeasurementUnit> builder)
    {
        builder.Property(e => e.Name).HasMaxLength(100).IsRequired();
        builder.Property(e => e.Symbol).HasMaxLength(20).IsRequired();
        builder.HasIndex(e => e.CategoryId);
        builder.HasIndex(e => e.UnitSystemId);
        builder.HasIndex(e => e.Symbol);
        builder.HasIndex(e => e.BaseUnitId);
    }
}
