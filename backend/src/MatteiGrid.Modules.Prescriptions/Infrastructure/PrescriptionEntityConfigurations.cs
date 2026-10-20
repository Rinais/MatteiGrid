using MatteiGrid.Modules.Prescriptions.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MatteiGrid.Modules.Prescriptions.Infrastructure;

public class MedicineConfiguration : IEntityTypeConfiguration<Medicine>
{
    public void Configure(EntityTypeBuilder<Medicine> builder)
    {
        builder.ToTable("Medicines");
        builder.HasKey(m => m.Id);
        builder.HasIndex(m => new { m.ClinicId, m.Id }).IsUnique();

        builder.Property(m => m.Name).IsRequired().HasMaxLength(200);
        builder.HasIndex(m => new { m.ClinicId, m.Name }).IsUnique();
    }
}

public class DilutionConfiguration : IEntityTypeConfiguration<Dilution>
{
    public void Configure(EntityTypeBuilder<Dilution> builder)
    {
        builder.ToTable("Dilutions");
        builder.HasKey(d => d.Id);
        builder.HasIndex(d => new { d.ClinicId, d.Id }).IsUnique();

        builder.Property(d => d.Label).IsRequired().HasMaxLength(50);
        builder.HasIndex(d => new { d.ClinicId, d.Label }).IsUnique();
    }
}

public class VehicleConfiguration : IEntityTypeConfiguration<Vehicle>
{
    public void Configure(EntityTypeBuilder<Vehicle> builder)
    {
        builder.ToTable("Vehicles");
        builder.HasKey(v => v.Id);
        builder.HasIndex(v => new { v.ClinicId, v.Id }).IsUnique();

        builder.Property(v => v.Name).IsRequired().HasMaxLength(100);
        builder.HasIndex(v => new { v.ClinicId, v.Name }).IsUnique();
    }
}

public class PrescriptionConfiguration : IEntityTypeConfiguration<Prescription>
{
    public void Configure(EntityTypeBuilder<Prescription> builder)
    {
        builder.ToTable("Prescriptions");
        builder.HasKey(p => p.Id);
        builder.HasIndex(p => new { p.ClinicId, p.Id }).IsUnique();

        builder.Property(p => p.Status).IsRequired().HasMaxLength(50);
        builder.Property(p => p.DietAndRegimenInstructions).HasMaxLength(1000);
        builder.Property(p => p.GeneralInstructions).HasMaxLength(1000);

        // ERD v4 Sec 1: UNIQUE(PreviousVersionId) WHERE PreviousVersionId IS NOT NULL (linear chain, no forks)
        builder.HasIndex(p => p.PreviousVersionId)
            .IsUnique()
            .HasFilter("\"PreviousVersionId\" IS NOT NULL");

        builder.HasOne(p => p.PreviousVersion)
            .WithOne()
            .HasForeignKey<Prescription>(p => p.PreviousVersionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(p => new { p.ClinicId, p.ConsultationId });
    }
}

public class PrescriptionItemConfiguration : IEntityTypeConfiguration<PrescriptionItem>
{
    public void Configure(EntityTypeBuilder<PrescriptionItem> builder)
    {
        builder.ToTable("PrescriptionItems");
        builder.HasKey(pi => pi.Id);

        builder.Property(pi => pi.ElectricityPolarity).IsRequired().HasMaxLength(20);
        builder.Property(pi => pi.Quantity).HasPrecision(18, 2);
        builder.Property(pi => pi.Dosage).IsRequired().HasMaxLength(100);
        builder.Property(pi => pi.Duration).IsRequired().HasMaxLength(100);
        builder.Property(pi => pi.SelectionMethod).IsRequired().HasMaxLength(50).HasDefaultValue(PrescriptionSelectionMethod.Repertorial);
        builder.Property(pi => pi.Frequency).IsRequired().HasMaxLength(50).HasDefaultValue("TDS");
        builder.Property(pi => pi.Instructions).HasMaxLength(500);

        // Composite Foreign Keys for defense-in-depth tenant isolation (ERD v4 Section 1 & 15.2)
        builder.HasOne(pi => pi.Prescription)
            .WithMany(p => p.Items)
            .HasForeignKey(pi => pi.PrescriptionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(pi => pi.Medicine)
            .WithMany()
            .HasForeignKey(pi => pi.MedicineId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(pi => pi.Dilution)
            .WithMany()
            .HasForeignKey(pi => pi.DilutionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(pi => pi.Vehicle)
            .WithMany()
            .HasForeignKey(pi => pi.VehicleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
