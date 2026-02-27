using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TheWatch.P2.VoiceEmergency.Emergency;

namespace TheWatch.P2.VoiceEmergency.Data.Configurations;

public class ChemicalLookupConfiguration : IEntityTypeConfiguration<ChemicalLookup>
{
    public void Configure(EntityTypeBuilder<ChemicalLookup> builder)
    {
        builder.Property(e => e.SubstanceName).HasMaxLength(300).IsRequired();
        builder.Property(e => e.SubstanceCasNumber).HasMaxLength(50);
        builder.Property(e => e.EstimatedQuantity).HasMaxLength(200);
        builder.Property(e => e.SymptomsReported).HasColumnType("nvarchar(max)");
        builder.Property(e => e.TreatmentProtocolId).HasMaxLength(100);
        builder.Property(e => e.PoisonControlCaseNum).HasMaxLength(100);
        builder.Property(e => e.LocationLatitude).HasPrecision(10, 7);
        builder.Property(e => e.LocationLongitude).HasPrecision(10, 7);
        builder.Property(e => e.LocationDescription).HasMaxLength(500);

        builder.HasIndex(e => new { e.UserId, e.IsDeleted });
        builder.HasIndex(e => new { e.SubstanceName, e.SubstanceCasNumber });
        builder.HasIndex(e => e.AlertId);
        builder.HasIndex(e => new { e.ExposureSeverity, e.ReportedAt }).IsDescending(false, true);

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_ChemicalLookup_Patients", "[PatientCount] >= 1");
        });
    }
}
