using MatteiGrid.Modules.Clinical.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MatteiGrid.Modules.Clinical.Infrastructure;

public class ConsultationConfiguration : IEntityTypeConfiguration<Consultation>
{
    public void Configure(EntityTypeBuilder<Consultation> builder)
    {
        builder.ToTable("Consultations");
        builder.HasKey(c => c.Id);
        builder.HasIndex(c => new { c.ClinicId, c.Id }).IsUnique();

        builder.Property(c => c.Status).IsRequired().HasMaxLength(50);
        builder.Property(c => c.CaseType).IsRequired().HasMaxLength(50);
        builder.Property(c => c.ProvisionalDiagnosis).HasMaxLength(500);
        builder.Property(c => c.FinalDiagnosis).HasMaxLength(500);
        builder.Property(c => c.Outcome).IsRequired().HasMaxLength(50);
        builder.HasIndex(c => new { c.ClinicId, c.PatientId });
        builder.HasIndex(c => new { c.ClinicId, c.DoctorStaffId });
    }
}

public class ConsultationObservationConfiguration : IEntityTypeConfiguration<ConsultationObservation>
{
    public void Configure(EntityTypeBuilder<ConsultationObservation> builder)
    {
        builder.ToTable("ConsultationObservations");
        builder.HasKey(o => o.Id);
        builder.HasIndex(o => new { o.ClinicId, o.Id }).IsUnique();

        builder.Property(o => o.ReactionType).IsRequired().HasMaxLength(100);
        builder.Property(o => o.KentsObservation).HasMaxLength(200);
        builder.Property(o => o.SecondPrescriptionAction).HasMaxLength(100);
        builder.Property(o => o.Notes).IsRequired();

        builder.HasOne(o => o.Consultation)
            .WithMany(c => c.Observations)
            .HasForeignKey(o => o.ConsultationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class ClinicalNoteConfiguration : IEntityTypeConfiguration<ClinicalNote>
{
    public void Configure(EntityTypeBuilder<ClinicalNote> builder)
    {
        builder.ToTable("ClinicalNotes");
        builder.HasKey(n => n.Id);
        builder.HasIndex(n => new { n.ClinicId, n.Id }).IsUnique();

        builder.Property(n => n.Content).IsRequired();

        builder.HasOne(n => n.Consultation)
            .WithMany(c => c.ClinicalNotes)
            .HasForeignKey(n => n.ConsultationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
