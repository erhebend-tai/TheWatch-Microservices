using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TheWatch.P8.DisasterRelief.Relief;

namespace TheWatch.P8.DisasterRelief.Data.Configurations;

public class ShelterConfiguration : IEntityTypeConfiguration<Shelter>
{
    public void Configure(EntityTypeBuilder<Shelter> builder)
    {
        builder.Property(e => e.Name).HasMaxLength(200).IsRequired();
        builder.Property(e => e.ContactPhone).HasMaxLength(50);
        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => e.DisasterEventId);
        builder.ToTable(t => {
            t.HasCheckConstraint("CK_Shelter_Capacity", "[Capacity] > 0");
            t.HasCheckConstraint("CK_Shelter_Occupancy", "[CurrentOccupancy] >= 0 AND [CurrentOccupancy] <= [Capacity]");
        });
    }
}
