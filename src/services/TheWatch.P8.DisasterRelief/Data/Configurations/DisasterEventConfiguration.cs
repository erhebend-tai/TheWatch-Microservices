using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TheWatch.P8.DisasterRelief.Relief;

namespace TheWatch.P8.DisasterRelief.Data.Configurations;

public class DisasterEventConfiguration : IEntityTypeConfiguration<DisasterEvent>
{
    public void Configure(EntityTypeBuilder<DisasterEvent> builder)
    {
        builder.Property(e => e.Name).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Description).HasMaxLength(4000);
        builder.HasIndex(e => e.Type);
        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => new { e.Status, e.CreatedAt }).IsDescending(false, true);
        builder.ToTable(t => t.HasCheckConstraint("CK_DisasterEvent_Severity", "[Severity] BETWEEN 1 AND 5"));
    }
}
