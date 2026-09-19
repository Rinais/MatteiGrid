using MatteiGrid.Modules.Appointments.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MatteiGrid.Modules.Appointments.Infrastructure;

public class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
{
    public void Configure(EntityTypeBuilder<Appointment> builder)
    {
        builder.ToTable("Appointments");
        builder.HasKey(a => a.Id);
        builder.HasIndex(a => new { a.ClinicId, a.Id }).IsUnique();

        builder.Property(a => a.Status).IsRequired().HasMaxLength(50);
        builder.Property(a => a.OverrideReason).HasMaxLength(500);

        builder.HasIndex(a => new { a.ClinicId, a.DoctorStaffId, a.StartAt, a.EndAt });
        builder.HasIndex(a => new { a.ClinicId, a.PatientId });
    }
}
