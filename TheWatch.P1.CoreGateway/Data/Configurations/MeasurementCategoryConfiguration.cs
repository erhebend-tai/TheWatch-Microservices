using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TheWatch.P1.CoreGateway.Measurements;

namespace TheWatch.P1.CoreGateway.Data.Configurations;

public class MeasurementCategoryConfiguration : IEntityTypeConfiguration<MeasurementCategory>
{
    public void Configure(EntityTypeBuilder<MeasurementCategory> builder)
    {
        builder.Property(e => e.Name).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Description).HasMaxLength(1000);
        builder.HasIndex(e => e.Domain);
        builder.HasIndex(e => e.ParentCategoryId);
        builder.HasIndex(e => e.Name).IsUnique();
        builder.HasIndex(e => e.IsActive);
    }
}
