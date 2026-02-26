using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TheWatch.P8.DisasterRelief.Relief;

namespace TheWatch.P8.DisasterRelief.Data.Configurations;

public class SupportGroupConfiguration : IEntityTypeConfiguration<SupportGroup>
{
    public void Configure(EntityTypeBuilder<SupportGroup> builder)
    {
        builder.Property(e => e.GroupName).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Description).HasMaxLength(2000);
        builder.Property(e => e.MeetingSchedule).HasMaxLength(500);
        builder.Property(e => e.MeetingLocation).HasMaxLength(500);
        builder.Property(e => e.MeetingLink).HasMaxLength(500);
        builder.HasIndex(e => e.DisasterEventId);
        builder.HasIndex(e => e.GroupType);
        builder.HasIndex(e => e.IsActive);
        builder.ToTable(t => t.HasCheckConstraint("CK_SupportGroup_Members", "[CurrentMembers] <= [MaxMembers]"));
    }
}
