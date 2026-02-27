using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TheWatch.P1.CoreGateway.Measurements;

namespace TheWatch.P1.CoreGateway.Data.Configurations;

public class UnitSystemConfiguration : IEntityTypeConfiguration<UnitSystem>
{
    public void Configure(EntityTypeBuilder<UnitSystem> builder)
    {
        builder.Property(e => e.Name).HasMaxLength(100).IsRequired();
        builder.Property(e => e.Abbreviation).HasMaxLength(20).IsRequired();
        builder.Property(e => e.Description).HasMaxLength(500);
        builder.HasIndex(e => e.Name).IsUnique();
        builder.HasIndex(e => e.Abbreviation).IsUnique();
    }
}
