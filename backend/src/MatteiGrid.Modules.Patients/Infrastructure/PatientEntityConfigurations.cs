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

public class HomeopathicCaseRecordConfiguration : IEntityTypeConfiguration<HomeopathicCaseRecord>
{
    public void Configure(EntityTypeBuilder<HomeopathicCaseRecord> builder)
    {
        builder.ToTable("HomeopathicCaseRecords");
        builder.HasKey(c => c.Id);

        // Tenancy Model A: Composite unique constraints (ClinicId, Id) and (ClinicId, PatientId)
        builder.HasIndex(c => new { c.ClinicId, c.Id }).IsUnique();
        builder.HasIndex(c => new { c.ClinicId, c.PatientId }).IsUnique();

        builder.Property(c => c.MaritalStatus).HasMaxLength(50);
        builder.Property(c => c.Occupation).HasMaxLength(100);
        builder.Property(c => c.Diet).HasMaxLength(50);
        builder.Property(c => c.Address).HasMaxLength(500);
        builder.Property(c => c.Email).HasMaxLength(150);
        builder.Property(c => c.EmergencyContactName).HasMaxLength(150);
        builder.Property(c => c.EmergencyContactPhone).HasMaxLength(20);
        builder.Property(c => c.EmergencyContactRelation).HasMaxLength(50);
        builder.Property(c => c.ReferredBy).HasMaxLength(150);
        builder.Property(c => c.CaseType).HasMaxLength(50);

        builder.Property(c => c.ThermalState).HasMaxLength(50);
        builder.Property(c => c.BloodPressure).HasMaxLength(20);

        builder.HasOne(c => c.Patient)
            .WithOne(p => p.CaseRecord)
            .HasForeignKey<HomeopathicCaseRecord>(c => c.PatientId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

