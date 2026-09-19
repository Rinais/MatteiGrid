namespace MatteiGrid.Modules.Laboratory.Contracts;

public record LaboratoryOrderDto(
    Guid Id,
    Guid ClinicId,
    Guid PrescriptionId,
    Guid? PreparedByStaffId,
    string Status,
    string? RejectionComment,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

public interface ILaboratoryService
{
    Task<LaboratoryOrderDto> CreateOrderForPrescriptionAsync(Guid prescriptionId, CancellationToken ct = default);
    Task<IReadOnlyList<LaboratoryOrderDto>> GetQueueAsync(string? statusFilter = null, CancellationToken ct = default);
    Task<LaboratoryOrderDto> TransitionStatusAsync(Guid orderId, string targetStatus, Guid? staffId = null, CancellationToken ct = default);
    Task<LaboratoryOrderDto> RejectOrderAsync(Guid orderId, string comment, Guid staffId, CancellationToken ct = default);
}