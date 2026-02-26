using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TheWatch.P8.DisasterRelief.Relief;

namespace TheWatch.P8.DisasterRelief.Data.Configurations;

public class DisasterVictimConfiguration : IEntityTypeConfiguration<DisasterVictim>
{
    public void Configure(EntityTypeBuilder<DisasterVictim> builder)
    {
        builder.Property(e => e.FullName).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Phone).HasMaxLength(50);
        builder.Property(e => e.Email).HasMaxLength(256);
        builder.Property(e => e.SpecialNeeds).HasMaxLength(2000);
        builder.Property(e => e.InsuranceInfo).HasMaxLength(500);
        builder.HasIndex(e => e.DisasterEventId);
        builder.HasIndex(e => e.UserId);
        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => e.CurrentShelterId);
        builder.ToTable(t => t.HasCheckConstraint("CK_DisasterVictim_Household", "[HouseholdSize] >= 1"));
    }
}
