namespace MatteiGrid.Modules.Inventory.Contracts;

public record InventoryItemDto(
    Guid Id,
    Guid ClinicId,
    Guid MedicineId,
    decimal QuantityOnHand,
    decimal ReorderLevel,
    bool IsLowStock,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

public record InventoryTransactionDto(
    Guid Id,
    Guid ClinicId,
    Guid InventoryItemId,
    Guid? LaboratoryOrderId,
    string TransactionType,
    decimal QuantityDelta,
    string Reason,
    DateTimeOffset CreatedAt);

public record ReceiveStockRequest(
    Guid MedicineId,
    decimal Quantity,
    string Reason);

public interface IInventoryService
{
    Task<IReadOnlyList<InventoryItemDto>> GetInventoryItemsAsync(bool onlyLowStock = false, CancellationToken ct = default);
    Task<InventoryItemDto> ReceiveStockAsync(ReceiveStockRequest request, CancellationToken ct = default);
    Task<InventoryTransactionDto> RecordLabDeductionAsync(Guid medicineId, decimal quantity, Guid laboratoryOrderId, CancellationToken ct = default);
    Task<IReadOnlyList<InventoryTransactionDto>> GetTransactionsAsync(Guid? inventoryItemId = null, CancellationToken ct = default);
}