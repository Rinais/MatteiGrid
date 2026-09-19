using MatteiGrid.Modules.Patients.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MatteiGrid.Modules.Patients.Infrastructure;

public class PatientConfiguration : IEntityTypeConfiguration<Patient>
{
    public void Configure(EntityTypeBuilder<Patient> builder)
    {
        builder.ToTable("Patients");
        builder.HasKey(p => p.Id);
        builder.HasIndex(p => new { p.ClinicId, p.Id }).IsUnique();

        builder.Property(p => p.Name).IsRequired().HasMaxLength(200);
        builder.Property(p => p.Phone).IsRequired().HasMaxLength(20);

        // PRD Section 7 & ERD: Patient phone number + name unique per clinic
        builder.HasIndex(p => new { p.ClinicId, p.Phone, p.Name }).IsUnique();
    }
}

public class ConsentRecordConfiguration : IEntityTypeConfiguration<ConsentRecord>
{
    public void Configure(EntityTypeBuilder<ConsentRecord> builder)
    {
        builder.ToTable("ConsentRecords");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.ConsentType).IsRequired().HasMaxLength(100);
        builder.Property(c => c.PolicyVersion).IsRequired().HasMaxLength(50);

        builder.HasOne(c => c.Patient)
            .WithMany(p => p.ConsentRecords)
            .HasForeignKey(c => c.PatientId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
