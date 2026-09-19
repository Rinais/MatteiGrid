using MatteiGrid.Shared.Kernel;

namespace MatteiGrid.Modules.Inventory.Domain;

public static class TransactionType
{
    public const string LabDeduction = "LabDeduction";
    public const string StockReceipt = "StockReceipt";
    public const string ManualAdjustment = "ManualAdjustment";
    public const string Wastage = "Wastage";
}

public class InventoryItem : ClinicScopedEntity
{
    public Guid MedicineId { get; set; }
    public decimal QuantityOnHand { get; set; }
    public decimal ReorderLevel { get; set; }

    public ICollection<InventoryTransaction> Transactions { get; set; } = new List<InventoryTransaction>();
}

public class InventoryTransaction : IClinicScopedEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ClinicId { get; set; }
    public Guid InventoryItemId { get; set; }
    public Guid? LaboratoryOrderId { get; set; } // Only set for LabDeduction per ERD v4
    public string TransactionType { get; set; } = Domain.TransactionType.LabDeduction;
    public decimal QuantityDelta { get; set; } // Signed delta per ERD v4 (negative for deductions)
    public string Reason { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow; // Append-only ledger — no UpdatedAt

    public InventoryItem? InventoryItem { get; set; }
}
