using MatteiGrid.Shared.Kernel;

namespace MatteiGrid.Modules.Appointments.Domain;

public static class AppointmentStatus
{
    public const string Scheduled = "Scheduled";
    public const string InProgress = "InProgress";
    public const string Completed = "Completed";
    public const string Cancelled = "Cancelled";
    public const string NoShow = "NoShow";
}

public class Appointment : ClinicScopedEntity
{
    public Guid PatientId { get; set; }
    public Guid DoctorStaffId { get; set; }
    public DateTimeOffset StartAt { get; set; }
    public DateTimeOffset EndAt { get; set; }
    public string Status { get; set; } = AppointmentStatus.Scheduled;
    public string? OverrideReason { get; set; } // Nullable — logs double-booking override reason per PRD 5.3
}
