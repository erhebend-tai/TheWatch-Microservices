using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TheWatch.P8.DisasterRelief.Relief;

namespace TheWatch.P8.DisasterRelief.Data.Configurations;

public class BuyingGroupConfiguration : IEntityTypeConfiguration<BuyingGroup>
{
    public void Configure(EntityTypeBuilder<BuyingGroup> builder)
    {
        builder.Property(e => e.GroupName).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Description).HasMaxLength(2000);
        builder.Property(e => e.TargetItem).HasMaxLength(200).IsRequired();
        builder.Property(e => e.EstimatedUnitPrice).HasPrecision(18, 2);
        builder.Property(e => e.CollectedAmount).HasPrecision(18, 2);
        builder.HasIndex(e => e.DisasterEventId);
        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => e.OrganizerId);
        builder.ToTable(t => {
            t.HasCheckConstraint("CK_BuyingGroup_Members", "[CurrentMembers] <= [MaxMembers]");
            t.HasCheckConstraint("CK_BuyingGroup_Quantity", "[TargetQuantity] > 0");
        });
    }
}
