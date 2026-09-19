namespace MatteiGrid.Modules.Patients.Contracts;

public record RegisterPatientRequest(string Name, string Phone, DateOnly DOB, string? ConsentType);
public record PatientDto(Guid Id, Guid ClinicId, string Name, string Phone, DateOnly DOB, bool IsArchived, bool IsAnonymized, DateTimeOffset CreatedAt);
public record DuplicatePatientWarning(bool HasPotentialDuplicate, string? Message, IReadOnlyList<PatientDto> Matches);

public interface IPatientService
{
    Task<(PatientDto Patient, DuplicatePatientWarning Warning)> RegisterPatientAsync(RegisterPatientRequest request, CancellationToken ct = default);
    Task<DuplicatePatientWarning> CheckDuplicateAsync(string name, string phone, DateOnly dob, CancellationToken ct = default);
    Task<IReadOnlyList<PatientDto>> SearchPatientsAsync(string? query, CancellationToken ct = default);
    Task<PatientDto?> GetPatientByIdAsync(Guid id, CancellationToken ct = default);
    Task<bool> ArchivePatientAsync(Guid id, CancellationToken ct = default);
    Task<bool> ErasePatientPiiAsync(Guid id, CancellationToken ct = default);
}