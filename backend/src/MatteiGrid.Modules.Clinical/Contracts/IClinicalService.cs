namespace MatteiGrid.Modules.Clinical.Contracts;

public record StartConsultationRequest(
    Guid PatientId,
    Guid DoctorStaffId,
    Guid? AppointmentId,
    string? CaseType = null,
    string? ProvisionalDiagnosis = null);

public record ClinicalNoteDto(Guid Id, Guid ConsultationId, Guid StaffId, string Content, DateTimeOffset CreatedAt, DateTimeOffset? UpdatedAt);

public record ConsultationObservationDto(
    Guid Id,
    Guid ConsultationId,
    Guid StaffId,
    DateTimeOffset ObservationDate,
    string ReactionType,
    string? KentsObservation,
    string? SecondPrescriptionAction,
    string Notes,
    DateTimeOffset CreatedAt);

public record UpdateConsultationClinicalDetailsRequest(
    string CaseType,
    string ProvisionalDiagnosis,
    string FinalDiagnosis,
    string Outcome);

public record AddConsultationObservationRequest(
    Guid StaffId,
    string ReactionType,
    string? KentsObservation,
    string? SecondPrescriptionAction,
    string Notes);

public record ConsultationDto(
    Guid Id,
    Guid ClinicId,
    Guid PatientId,
    Guid DoctorStaffId,
    Guid? AppointmentId,
    string Status,
    string CaseType,
    string ProvisionalDiagnosis,
    string FinalDiagnosis,
    string Outcome,
    DateTimeOffset StartedAt,
    DateTimeOffset? CompletedAt,
    IReadOnlyList<ClinicalNoteDto> Notes,
    IReadOnlyList<ConsultationObservationDto> Observations);

public interface IClinicalService
{
    Task<ConsultationDto> StartConsultationAsync(StartConsultationRequest request, CancellationToken ct = default);
    Task<ConsultationDto?> GetConsultationAsync(Guid consultationId, CancellationToken ct = default);
    Task<ConsultationDto?> GetActiveOrLatestConsultationForPatientAsync(Guid patientId, CancellationToken ct = default);
    Task<ClinicalNoteDto> AddOrUpdateNoteAsync(Guid consultationId, Guid staffId, string content, Guid? noteId = null, CancellationToken ct = default);
    Task<ConsultationDto?> UpdateClinicalDetailsAsync(Guid consultationId, UpdateConsultationClinicalDetailsRequest request, CancellationToken ct = default);
    Task<ConsultationObservationDto> AddObservationAsync(Guid consultationId, AddConsultationObservationRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<ConsultationObservationDto>> GetObservationsAsync(Guid consultationId, CancellationToken ct = default);
    Task<bool> CompleteConsultationAsync(Guid consultationId, CancellationToken ct = default);
}