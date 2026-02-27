using Microsoft.EntityFrameworkCore;
using TheWatch.P1.CoreGateway.Measurements;

namespace TheWatch.P1.CoreGateway.Data.Seeders;

public class MeasurementSeeder
{
    public static async Task SeedAsync(DbContext context, CancellationToken ct = default)
    {
        // Unit Systems
        var si = new UnitSystem { Id = Guid.Parse("00000000-0000-0000-0000-000000000001"), Name = "International System (SI)", Abbreviation = "SI", Description = "Metric system", IsMetric = true };
        var imperial = new UnitSystem { Id = Guid.Parse("00000000-0000-0000-0000-000000000002"), Name = "Imperial", Abbreviation = "IMP", Description = "Imperial/US customary system", IsMetric = false };
        var cgs = new UnitSystem { Id = Guid.Parse("00000000-0000-0000-0000-000000000003"), Name = "CGS", Abbreviation = "CGS", Description = "Centimetre-gram-second system", IsMetric = true };

        if (!await context.Set<UnitSystem>().AnyAsync(ct))
        {
            context.Set<UnitSystem>().AddRange(si, imperial, cgs);
            await context.SaveChangesAsync(ct);
        }

        // Categories
        var physical = new MeasurementCategory { Id = Guid.Parse("00000000-0000-0000-0000-000000000010"), Name = "Physical", Domain = MeasurementDomain.Physical, SortOrder = 1 };
        var chemical = new MeasurementCategory { Id = Guid.Parse("00000000-0000-0000-0000-000000000011"), Name = "Chemical", Domain = MeasurementDomain.Chemical, SortOrder = 2 };
        var environmental = new MeasurementCategory { Id = Guid.Parse("00000000-0000-0000-0000-000000000012"), Name = "Environmental", Domain = MeasurementDomain.Environmental, SortOrder = 3 };
        var medical = new MeasurementCategory { Id = Guid.Parse("00000000-0000-0000-0000-000000000013"), Name = "Medical", Domain = MeasurementDomain.Medical, SortOrder = 4 };
        var signal = new MeasurementCategory { Id = Guid.Parse("00000000-0000-0000-0000-000000000014"), Name = "Signal", Domain = MeasurementDomain.Signal, SortOrder = 5 };
        var wave = new MeasurementCategory { Id = Guid.Parse("00000000-0000-0000-0000-000000000015"), Name = "Wave", Domain = MeasurementDomain.Wave, SortOrder = 6 };

        if (!await context.Set<MeasurementCategory>().AnyAsync(ct))
        {
            context.Set<MeasurementCategory>().AddRange(physical, chemical, environmental, medical, signal, wave);
            await context.SaveChangesAsync(ct);
        }

        // Core Units
        if (!await context.Set<MeasurementUnit>().AnyAsync(ct))
        {
            context.Set<MeasurementUnit>().AddRange(
                new MeasurementUnit { Id = Guid.Parse("00000000-0000-0000-0000-000000000100"), Name = "Meter", Symbol = "m", UnitType = UnitType.Base, CategoryId = physical.Id, UnitSystemId = si.Id, IsBaseUnit = true },
                new MeasurementUnit { Id = Guid.Parse("00000000-0000-0000-0000-000000000101"), Name = "Kilogram", Symbol = "kg", UnitType = UnitType.Base, CategoryId = physical.Id, UnitSystemId = si.Id, IsBaseUnit = true },
                new MeasurementUnit { Id = Guid.Parse("00000000-0000-0000-0000-000000000102"), Name = "Second", Symbol = "s", UnitType = UnitType.Base, CategoryId = physical.Id, UnitSystemId = si.Id, IsBaseUnit = true },
                new MeasurementUnit { Id = Guid.Parse("00000000-0000-0000-0000-000000000103"), Name = "Kelvin", Symbol = "K", UnitType = UnitType.Base, CategoryId = physical.Id, UnitSystemId = si.Id, IsBaseUnit = true },
                new MeasurementUnit { Id = Guid.Parse("00000000-0000-0000-0000-000000000104"), Name = "Celsius", Symbol = "\u00b0C", UnitType = UnitType.Derived, CategoryId = physical.Id, UnitSystemId = si.Id, BaseUnitId = Guid.Parse("00000000-0000-0000-0000-000000000103"), ConversionOffset = 273.15 },
                new MeasurementUnit { Id = Guid.Parse("00000000-0000-0000-0000-000000000105"), Name = "Hertz", Symbol = "Hz", UnitType = UnitType.Derived, CategoryId = signal.Id, UnitSystemId = si.Id, IsBaseUnit = true },
                new MeasurementUnit { Id = Guid.Parse("00000000-0000-0000-0000-000000000106"), Name = "Decibel", Symbol = "dB", UnitType = UnitType.Derived, CategoryId = signal.Id, UnitSystemId = si.Id, IsBaseUnit = true },
                new MeasurementUnit { Id = Guid.Parse("00000000-0000-0000-0000-000000000107"), Name = "Pascal", Symbol = "Pa", UnitType = UnitType.Derived, CategoryId = environmental.Id, UnitSystemId = si.Id, IsBaseUnit = true },
                new MeasurementUnit { Id = Guid.Parse("00000000-0000-0000-0000-000000000108"), Name = "Beats per Minute", Symbol = "bpm", UnitType = UnitType.Custom, CategoryId = medical.Id, UnitSystemId = si.Id, IsBaseUnit = true },
                new MeasurementUnit { Id = Guid.Parse("00000000-0000-0000-0000-000000000109"), Name = "Nanometer", Symbol = "nm", UnitType = UnitType.Derived, CategoryId = wave.Id, UnitSystemId = si.Id, BaseUnitId = Guid.Parse("00000000-0000-0000-0000-000000000100"), ConversionFactor = 1e-9 }
            );
            await context.SaveChangesAsync(ct);
        }

        // Measurement Ranges
        if (!await context.Set<MeasurementRange>().AnyAsync(ct))
        {
            context.Set<MeasurementRange>().AddRange(
                new MeasurementRange { Id = Guid.Parse("00000000-0000-0000-0000-000000000200"), UnitId = Guid.Parse("00000000-0000-0000-0000-000000000108"), Name = "Adult Resting Heart Rate", MinValue = 40, MaxValue = 200, WarningLow = 50, WarningHigh = 100, CriticalLow = 40, CriticalHigh = 150, Context = "Adult at rest" },
                new MeasurementRange { Id = Guid.Parse("00000000-0000-0000-0000-000000000201"), UnitId = Guid.Parse("00000000-0000-0000-0000-000000000104"), Name = "Human Body Temperature", MinValue = 35.0, MaxValue = 42.0, WarningLow = 36.0, WarningHigh = 37.5, CriticalLow = 35.0, CriticalHigh = 40.0, Context = "Oral measurement" },
                new MeasurementRange { Id = Guid.Parse("00000000-0000-0000-0000-000000000202"), UnitId = Guid.Parse("00000000-0000-0000-0000-000000000106"), Name = "Ambient Noise Level", MinValue = 0, MaxValue = 194, WarningHigh = 85, CriticalHigh = 120, Context = "Occupational safety" }
            );
            await context.SaveChangesAsync(ct);
        }
    }
}
