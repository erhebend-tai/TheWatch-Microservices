using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TheWatch.P1.CoreGateway.Measurements;

namespace TheWatch.P1.CoreGateway.Data.Configurations;

public class MeasuringDeviceConfiguration : IEntityTypeConfiguration<MeasuringDevice>
{
    public void Configure(EntityTypeBuilder<MeasuringDevice> builder)
    {
        builder.Property(e => e.Name).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Manufacturer).HasMaxLength(200);
        builder.Property(e => e.ModelNumber).HasMaxLength(100);
        builder.Property(e => e.SerialNumber).HasMaxLength(100);
        builder.Property(e => e.CalibrationSchedule).HasMaxLength(100);
        builder.Property(e => e.FirmwareVersion).HasMaxLength(50);
        builder.HasIndex(e => e.DeviceType);
        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => e.PrimaryCategoryId);
        builder.HasIndex(e => e.OwnerId);
        builder.HasIndex(e => e.SerialNumber);
    }
}
