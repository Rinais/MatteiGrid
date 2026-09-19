using MatteiGrid.Shared.Kernel;

namespace MatteiGrid.Modules.Clinical.Domain;

public static class ConsultationStatus
{
    public const string Scheduled = "Scheduled";
    public const string InProgress = "InProgress";
    public const string Completed = "Completed";
    public const string Cancelled = "Cancelled";
    public const string NoShow = "NoShow";
}

public class Consultation : IClinicScopedEntity, IAuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ClinicId { get; set; }
    public Guid PatientId { get; set; }
    public Guid DoctorStaffId { get; set; }
    public Guid? AppointmentId { get; set; } // Nullable for walk-ins
    public string Status { get; set; } = ConsultationStatus.InProgress;
    public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }

    public ICollection<ClinicalNote> ClinicalNotes { get; set; } = new List<ClinicalNote>();
}

public class ClinicalNote : ClinicScopedEntity
{
    public Guid ConsultationId { get; set; }
    public Guid StaffId { get; set; }
    public string Content { get; set; } = string.Empty;

    public Consultation? Consultation { get; set; }
}
