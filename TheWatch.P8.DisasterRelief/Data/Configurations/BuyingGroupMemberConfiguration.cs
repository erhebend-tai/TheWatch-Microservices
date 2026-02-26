using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TheWatch.P8.DisasterRelief.Relief;

namespace TheWatch.P8.DisasterRelief.Data.Configurations;

public class BuyingGroupMemberConfiguration : IEntityTypeConfiguration<BuyingGroupMember>
{
    public void Configure(EntityTypeBuilder<BuyingGroupMember> builder)
    {
        builder.Property(e => e.ContributionAmount).HasPrecision(18, 2);
        builder.HasIndex(e => e.GroupId);
        builder.HasIndex(e => e.UserId);
        builder.HasIndex(e => new { e.GroupId, e.UserId }).IsUnique();
        builder.ToTable(t => t.HasCheckConstraint("CK_BuyingGroupMember_Quantity", "[QuantityRequested] >= 1"));
    }
}
