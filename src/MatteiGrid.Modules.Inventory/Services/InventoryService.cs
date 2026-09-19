using MatteiGrid.Modules.Inventory.Contracts;
using MatteiGrid.Modules.Inventory.Domain;
using MatteiGrid.Shared.Infrastructure.Persistence;
using MatteiGrid.Shared.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace MatteiGrid.Modules.Inventory.Services;

public class InventoryService : IInventoryService
{
    private readonly AppDbContext _dbContext;
    private readonly ICurrentTenantContext _tenantContext;

    public InventoryService(AppDbContext dbContext, ICurrentTenantContext tenantContext)
    {
        _dbContext = dbContext;
        _tenantContext = tenantContext;
    }

    public async Task<IReadOnlyList<InventoryItemDto>> GetInventoryItemsAsync(bool onlyLowStock = false, CancellationToken ct = default)
    {
        var q = _dbContext.Set<InventoryItem>().AsQueryable();

        if (onlyLowStock)
        {
            q = q.Where(i => i.QuantityOnHand <= i.ReorderLevel);
        }

        var items = await q.OrderBy(i => i.QuantityOnHand).ToListAsync(ct);
        return items.Select(MapDto).ToList();
    }

    public async Task<InventoryItemDto> ReceiveStockAsync(ReceiveStockRequest request, CancellationToken ct = default)
    {
        if (request.Quantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero.", nameof(request.Quantity));

        var item = await _dbContext.Set<InventoryItem>()
            .FirstOrDefaultAsync(i => i.MedicineId == request.MedicineId, ct);

        if (item is null)
        {
            item = new InventoryItem
            {
                ClinicId = _tenantContext.ClinicId ?? Guid.Empty,
                MedicineId = request.MedicineId,
                QuantityOnHand = request.Quantity,
                ReorderLevel = 10m
            };
            _dbContext.Set<InventoryItem>().Add(item);
        }
        else
        {
            item.QuantityOnHand += request.Quantity;
            item.UpdatedAt = DateTimeOffset.UtcNow;
        }

        var tx = new InventoryTransaction
        {
            ClinicId = _tenantContext.ClinicId ?? Guid.Empty,
            InventoryItemId = item.Id,
            LaboratoryOrderId = null,
            TransactionType = TransactionType.StockReceipt,
            QuantityDelta = request.Quantity,
            Reason = string.IsNullOrWhiteSpace(request.Reason) ? "Stock receipt" : request.Reason
        };

        _dbContext.Set<InventoryTransaction>().Add(tx);
        await _dbContext.SaveChangesAsync(ct);

        return MapDto(item);
    }

    public async Task<InventoryTransactionDto> RecordLabDeductionAsync(Guid medicineId, decimal quantity, Guid laboratoryOrderId, CancellationToken ct = default)
    {
        if (quantity <= 0)
            throw new ArgumentException("Deduction quantity must be positive.", nameof(quantity));

        var item = await _dbContext.Set<InventoryItem>()
            .FirstOrDefaultAsync(i => i.MedicineId == medicineId, ct);

        if (item is null)
        {
            item = new InventoryItem
            {
                ClinicId = _tenantContext.ClinicId ?? Guid.Empty,
                MedicineId = medicineId,
                QuantityOnHand = 0,
                ReorderLevel = 10m
            };
            _dbContext.Set<InventoryItem>().Add(item);
        }

        item.QuantityOnHand -= quantity;
        item.UpdatedAt = DateTimeOffset.UtcNow;

        var tx = new InventoryTransaction
        {
            ClinicId = _tenantContext.ClinicId ?? Guid.Empty,
            InventoryItemId = item.Id,
            LaboratoryOrderId = laboratoryOrderId,
            TransactionType = TransactionType.LabDeduction,
            QuantityDelta = -quantity,
            Reason = $"Lab order preparation deduction (Order: {laboratoryOrderId})"
        };

        _dbContext.Set<InventoryTransaction>().Add(tx);
        await _dbContext.SaveChangesAsync(ct);

        return new InventoryTransactionDto(
            tx.Id,
            tx.ClinicId,
            tx.InventoryItemId,
            tx.LaboratoryOrderId,
            tx.TransactionType,
            tx.QuantityDelta,
            tx.Reason,
            tx.CreatedAt);
    }

    public async Task<IReadOnlyList<InventoryTransactionDto>> GetTransactionsAsync(Guid? inventoryItemId = null, CancellationToken ct = default)
    {
        var q = _dbContext.Set<InventoryTransaction>().AsQueryable();

        if (inventoryItemId.HasValue)
        {
            q = q.Where(t => t.InventoryItemId == inventoryItemId.Value);
        }

        var list = await q.OrderByDescending(t => t.CreatedAt).Take(100).ToListAsync(ct);

        return list.Select(t => new InventoryTransactionDto(
            t.Id,
            t.ClinicId,
            t.InventoryItemId,
            t.LaboratoryOrderId,
            t.TransactionType,
            t.QuantityDelta,
            t.Reason,
            t.CreatedAt)).ToList();
    }

    private static InventoryItemDto MapDto(InventoryItem i) =>
        new(i.Id, i.ClinicId, i.MedicineId, i.QuantityOnHand, i.ReorderLevel, i.QuantityOnHand <= i.ReorderLevel, i.CreatedAt, i.UpdatedAt);
}