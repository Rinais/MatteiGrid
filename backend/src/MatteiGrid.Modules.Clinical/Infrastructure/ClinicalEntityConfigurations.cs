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
        builder.HasIndex(c => new { c.ClinicId, c.PatientId });
        builder.HasIndex(c => new { c.ClinicId, c.DoctorStaffId });
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
