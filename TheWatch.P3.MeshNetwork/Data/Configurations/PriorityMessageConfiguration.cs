using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TheWatch.P3.MeshNetwork.Mesh;

namespace TheWatch.P3.MeshNetwork.Data.Configurations;

public class PriorityMessageConfiguration : IEntityTypeConfiguration<PriorityMessage>
{
    public void Configure(EntityTypeBuilder<PriorityMessage> builder)
    {
        builder.Property(e => e.PriorityReason).HasMaxLength(200).IsRequired();
        builder.HasIndex(e => e.BroadcastId);
        builder.HasIndex(e => e.PriorityLevel);
        builder.HasIndex(e => e.ProcessingStatus);
        builder.HasIndex(e => e.EscalationLevel);
        builder.HasIndex(e => e.CreatedAt).IsDescending();
        builder.ToTable(t => {
            t.HasCheckConstraint("CK_PriorityMessage_Level", "[PriorityLevel] BETWEEN 1 AND 5");
            t.HasCheckConstraint("CK_PriorityMessage_Escalation", "[EscalationLevel] <= [MaxEscalation]");
        });
    }
}
