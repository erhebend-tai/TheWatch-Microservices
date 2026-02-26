using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TheWatch.P8.DisasterRelief.Relief;

namespace TheWatch.P8.DisasterRelief.Data.Configurations;

public class SupportGroupMemberConfiguration : IEntityTypeConfiguration<SupportGroupMember>
{
    public void Configure(EntityTypeBuilder<SupportGroupMember> builder)
    {
        builder.HasIndex(e => e.GroupId);
        builder.HasIndex(e => e.UserId);
        builder.HasIndex(e => new { e.GroupId, e.UserId }).IsUnique();
        builder.HasIndex(e => e.IsActive);
    }
}
