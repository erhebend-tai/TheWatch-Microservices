using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TheWatch.P3.MeshNetwork.Mesh;

namespace TheWatch.P3.MeshNetwork.Data.Configurations;

public class MessageQueueEntryConfiguration : IEntityTypeConfiguration<MessageQueueEntry>
{
    public void Configure(EntityTypeBuilder<MessageQueueEntry> builder)
    {
        builder.Property(e => e.QueueName).HasMaxLength(100).IsRequired();
        builder.Property(e => e.MessageType).HasMaxLength(50).IsRequired();
        builder.Property(e => e.ErrorMessage).HasMaxLength(500);
        builder.Property(e => e.CorrelationId).HasMaxLength(128);
        builder.HasIndex(e => e.QueueStatus);
        builder.HasIndex(e => new { e.PriorityLevel, e.FirstEnqueuedAt });
        builder.HasIndex(e => e.VisibleAt);
        builder.HasIndex(e => e.SenderNodeId);
        builder.HasIndex(e => e.TargetNodeId);
        builder.HasIndex(e => e.ExpiresAt);
        builder.HasIndex(e => e.QueueName);
        builder.HasIndex(e => e.CorrelationId);
        builder.ToTable(t => {
            t.HasCheckConstraint("CK_MessageQueueEntry_Priority", "[PriorityLevel] BETWEEN 1 AND 5");
            t.HasCheckConstraint("CK_MessageQueueEntry_Dequeue", "[DequeueCount] <= [MaxDequeueCount]");
        });
    }
}
