using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TheWatch.P2.VoiceEmergency.Emergency;

namespace TheWatch.P2.VoiceEmergency.Data.Configurations;

public class EmergencyContactConfiguration : IEntityTypeConfiguration<EmergencyContact>
{
    public void Configure(EntityTypeBuilder<EmergencyContact> builder)
    {
        builder.Property(e => e.ContactName).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Phone).HasMaxLength(50);
        builder.Property(e => e.Email).HasMaxLength(256);
        builder.Property(e => e.Relationship).HasMaxLength(100).IsRequired();

        builder.HasIndex(e => new { e.UserId, e.IsActive, e.IsDeleted });
        builder.HasIndex(e => new { e.UserId, e.Priority });

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_EmergencyContact_Priority", "[Priority] BETWEEN 1 AND 10");
        });
    }
}
