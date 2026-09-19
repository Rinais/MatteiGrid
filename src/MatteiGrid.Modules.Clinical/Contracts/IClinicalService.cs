namespace MatteiGrid.Modules.Clinical.Contracts;

public record StartConsultationRequest(Guid PatientId, Guid DoctorStaffId, Guid? AppointmentId);
public record ClinicalNoteDto(Guid Id, Guid ConsultationId, Guid StaffId, string Content, DateTimeOffset CreatedAt, DateTimeOffset? UpdatedAt);
public record ConsultationDto(Guid Id, Guid ClinicId, Guid PatientId, Guid DoctorStaffId, Guid? AppointmentId, string Status, DateTimeOffset StartedAt, DateTimeOffset? CompletedAt, IReadOnlyList<ClinicalNoteDto> Notes);

public interface IClinicalService
{
    Task<ConsultationDto> StartConsultationAsync(StartConsultationRequest request, CancellationToken ct = default);
    Task<ConsultationDto?> GetConsultationAsync(Guid consultationId, CancellationToken ct = default);
    Task<ClinicalNoteDto> AddOrUpdateNoteAsync(Guid consultationId, Guid staffId, string content, Guid? noteId = null, CancellationToken ct = default);
    Task<bool> CompleteConsultationAsync(Guid consultationId, CancellationToken ct = default);
}