using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TheWatch.P1.CoreGateway.Measurements;

namespace TheWatch.P1.CoreGateway.Data.Configurations;

public class WaveMeasurementConfiguration : IEntityTypeConfiguration<WaveMeasurement>
{
    public void Configure(EntityTypeBuilder<WaveMeasurement> builder)
    {
        builder.Property(e => e.Medium).HasMaxLength(100);
        builder.Property(e => e.Source).HasMaxLength(200);
        builder.HasIndex(e => e.DeviceId);
        builder.HasIndex(e => e.UnitId);
        builder.HasIndex(e => e.WaveType);
        builder.HasIndex(e => e.MeasuredAt).IsDescending();
        builder.HasIndex(e => new { e.DeviceId, e.MeasuredAt }).IsDescending(false, true);
    }
}
