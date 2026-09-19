namespace MatteiGrid.Modules.Appointments.Contracts;

public record BookAppointmentRequest(Guid PatientId, Guid DoctorStaffId, DateTimeOffset StartAt, DateTimeOffset EndAt, string? OverrideReason);
public record AppointmentDto(Guid Id, Guid ClinicId, Guid PatientId, Guid DoctorStaffId, DateTimeOffset StartAt, DateTimeOffset EndAt, string Status, string? OverrideReason, DateTimeOffset CreatedAt);
public record OverlapCheckResult(bool HasOverlap, string? ConflictDetails);

public interface IAppointmentService
{
    Task<OverlapCheckResult> CheckOverlapAsync(Guid doctorStaffId, DateTimeOffset startAt, DateTimeOffset endAt, Guid? excludeAppointmentId = null, CancellationToken ct = default);
    Task<(AppointmentDto Appointment, OverlapCheckResult Overlap)> BookAppointmentAsync(BookAppointmentRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<AppointmentDto>> GetAppointmentsAsync(DateTimeOffset? from, DateTimeOffset? to, Guid? doctorStaffId, CancellationToken ct = default);
    Task<bool> UpdateStatusAsync(Guid appointmentId, string status, CancellationToken ct = default);
}