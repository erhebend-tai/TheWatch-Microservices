using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TheWatch.P8.DisasterRelief.Relief;

namespace TheWatch.P8.DisasterRelief.Data.Configurations;

public class MedicalConditionConfiguration : IEntityTypeConfiguration<MedicalCondition>
{
    public void Configure(EntityTypeBuilder<MedicalCondition> builder)
    {
        builder.Property(e => e.ConditionName).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Description).HasMaxLength(2000);
        builder.Property(e => e.MedicationsRequired).HasMaxLength(1000);
        builder.Property(e => e.TreatmentSchedule).HasMaxLength(500);
        builder.Property(e => e.AllergiesNotes).HasMaxLength(1000);
        builder.HasIndex(e => e.VictimId);
        builder.HasIndex(e => e.Severity);
        builder.HasIndex(e => e.NeedsEvacPriority);
    }
}
