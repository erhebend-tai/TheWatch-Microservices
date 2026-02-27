using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TheWatch.P3.MeshNetwork.Mesh;

namespace TheWatch.P3.MeshNetwork.Data.Configurations;

public class MessageTemplateConfiguration : IEntityTypeConfiguration<MessageTemplate>
{
    public void Configure(EntityTypeBuilder<MessageTemplate> builder)
    {
        builder.Property(e => e.TemplateCode).HasMaxLength(100).IsRequired();
        builder.Property(e => e.TemplateName).HasMaxLength(200).IsRequired();
        builder.Property(e => e.TemplateCategory).HasMaxLength(100).IsRequired();
        builder.Property(e => e.TemplateBody).HasMaxLength(500).IsRequired();
        builder.Property(e => e.TemplateBodyShort).HasMaxLength(140);
        builder.Property(e => e.PlaceholderSchema).HasColumnType("nvarchar(max)");
        builder.Property(e => e.LanguageCode).HasMaxLength(10);
        builder.Property(e => e.IconName).HasMaxLength(50);
        builder.Property(e => e.ColorCode).HasMaxLength(7);
        builder.HasIndex(e => e.TemplateCode).IsUnique();
        builder.HasIndex(e => e.TemplateCategory);
        builder.HasIndex(e => e.MessageTypeId);
        builder.HasIndex(e => e.IsEmergencyTemplate);
        builder.HasIndex(e => e.IsEnabled);
        builder.ToTable(t => {
            t.HasCheckConstraint("CK_MessageTemplate_Priority", "[DefaultPriority] BETWEEN 1 AND 5");
        });
    }
}
