using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TheWatch.P1.CoreGateway.Measurements;

namespace TheWatch.P1.CoreGateway.Data.Configurations;

public class SignalMeasurementConfiguration : IEntityTypeConfiguration<SignalMeasurement>
{
    public void Configure(EntityTypeBuilder<SignalMeasurement> builder)
    {
        builder.Property(e => e.Source).HasMaxLength(200);
        builder.HasIndex(e => e.DeviceId);
        builder.HasIndex(e => e.UnitId);
        builder.HasIndex(e => e.SignalType);
        builder.HasIndex(e => e.MeasuredAt).IsDescending();
        builder.HasIndex(e => new { e.DeviceId, e.MeasuredAt }).IsDescending(false, true);
    }
}
