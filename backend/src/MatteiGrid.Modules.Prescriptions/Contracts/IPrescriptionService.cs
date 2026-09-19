namespace MatteiGrid.Modules.Prescriptions.Contracts;

public record CreatePrescriptionItemRequest(
    Guid MedicineId,
    Guid DilutionId,
    Guid VehicleId,
    string ElectricityPolarity,
    decimal Quantity,
    string Dosage,
    string Duration);

public record CreatePrescriptionRequest(
    Guid ConsultationId,
    IReadOnlyList<CreatePrescriptionItemRequest> Items);

public record RevisePrescriptionRequest(
    Guid PreviousPrescriptionId,
    IReadOnlyList<CreatePrescriptionItemRequest> Items);

public record PrescriptionItemDto(
    Guid Id,
    Guid MedicineId,
    string MedicineName,
    Guid DilutionId,
    string DilutionLabel,
    Guid VehicleId,
    string VehicleName,
    string ElectricityPolarity,
    decimal Quantity,
    string Dosage,
    string Duration);

public record PrescriptionDto(
    Guid Id,
    Guid ClinicId,
    Guid ConsultationId,
    Guid? PreviousVersionId,
    int Version,
    string Status,
    Guid? FinalizedByStaffId,
    DateTimeOffset CreatedAt,
    DateTimeOffset? FinalizedAt,
    IReadOnlyList<PrescriptionItemDto> Items);

public interface IPrescriptionService
{
    Task<PrescriptionDto> CreatePrescriptionAsync(CreatePrescriptionRequest request, CancellationToken ct = default);
    Task<PrescriptionDto> RevisePrescriptionAsync(RevisePrescriptionRequest request, CancellationToken ct = default);
    Task<PrescriptionDto> FinalizePrescriptionAsync(Guid prescriptionId, Guid staffId, CancellationToken ct = default);
    Task<PrescriptionDto?> GetPrescriptionByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<PrescriptionDto>> GetPrescriptionsByConsultationIdAsync(Guid consultationId, CancellationToken ct = default);
}