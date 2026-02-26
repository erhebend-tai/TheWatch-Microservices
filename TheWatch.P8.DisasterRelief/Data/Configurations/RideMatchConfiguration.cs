using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TheWatch.P8.DisasterRelief.Relief;

namespace TheWatch.P8.DisasterRelief.Data.Configurations;

public class RideMatchConfiguration : IEntityTypeConfiguration<RideMatch>
{
    public void Configure(EntityTypeBuilder<RideMatch> builder)
    {
        builder.Property(e => e.Notes).HasMaxLength(1000);
        builder.HasIndex(e => e.RideId);
        builder.HasIndex(e => e.PassengerId);
        builder.HasIndex(e => e.Status);
        builder.ToTable(t => t.HasCheckConstraint("CK_RideMatch_Seats", "[SeatsRequested] >= 1"));
    }
}
