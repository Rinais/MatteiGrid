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

public static class CaseTypes
{
    public const string Acute = "Acute";
    public const string Chronic = "Chronic";
    public const string Gynaecological = "Gynaecological";
    public const string Surgical = "Surgical";
    public const string Epidemic = "Epidemic";
    public const string Others = "Others";

    public static readonly IReadOnlyList<string> All = new[] { Acute, Chronic, Gynaecological, Surgical, Epidemic, Others };
}

public static class CaseOutcomes
{
    public const string UnderTreatment = "UnderTreatment";
    public const string Cured = "Cured";
    public const string Relieved = "Relieved";
    public const string Referred = "Referred";
    public const string Otherwise = "Otherwise";
    public const string Expired = "Expired";

    public static readonly IReadOnlyList<string> All = new[] { UnderTreatment, Cured, Relieved, Referred, Otherwise, Expired };
}

public static class ReactionTypes
{
    public const string InitialEncounter = "InitialEncounter";
    public const string Amelioration = "Amelioration";
    public const string HomoeopathicAggravation = "HomoeopathicAggravation";
    public const string DiseaseAggravation = "DiseaseAggravation";
    public const string NoChange = "NoChange";
    public const string NewSymptoms = "NewSymptoms";

    public static readonly IReadOnlyList<string> All = new[] { InitialEncounter, Amelioration, HomoeopathicAggravation, DiseaseAggravation, NoChange, NewSymptoms };
}

public static class SecondPrescriptionActions
{
    public const string Continue = "Continue";
    public const string WaitAndWatch = "WaitAndWatch";
    public const string ChangePotency = "ChangePotency";
    public const string ChangeRemedy = "ChangeRemedy";

    public static readonly IReadOnlyList<string> All = new[] { Continue, WaitAndWatch, ChangePotency, ChangeRemedy };
}

public class Consultation : IClinicScopedEntity, IAuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ClinicId { get; set; }
    public Guid PatientId { get; set; }
    public Guid DoctorStaffId { get; set; }
    public Guid? AppointmentId { get; set; } // Nullable for walk-ins
    public string Status { get; set; } = ConsultationStatus.InProgress;

    // Organon Case Sheet Page 2 & 6 Enhancements
    public string CaseType { get; set; } = CaseTypes.Chronic;
    public string ProvisionalDiagnosis { get; set; } = string.Empty;
    public string FinalDiagnosis { get; set; } = string.Empty;
    public string Outcome { get; set; } = CaseOutcomes.UnderTreatment;

    public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }

    public ICollection<ClinicalNote> ClinicalNotes { get; set; } = new List<ClinicalNote>();
    public ICollection<ConsultationObservation> Observations { get; set; } = new List<ConsultationObservation>();
}

public class ClinicalNote : ClinicScopedEntity
{
    public Guid ConsultationId { get; set; }
    public Guid StaffId { get; set; }
    public string Content { get; set; } = string.Empty;

    public Consultation? Consultation { get; set; }
}

public class ConsultationObservation : ClinicScopedEntity
{
    public Guid ConsultationId { get; set; }
    public Guid StaffId { get; set; }
    public DateTimeOffset ObservationDate { get; set; } = DateTimeOffset.UtcNow;
    public string ReactionType { get; set; } = ReactionTypes.InitialEncounter;
    public string? KentsObservation { get; set; }
    public string? SecondPrescriptionAction { get; set; }
    public string Notes { get; set; } = string.Empty;

    public Consultation? Consultation { get; set; }
}
