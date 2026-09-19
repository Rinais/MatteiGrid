using MatteiGrid.Modules.Laboratory.Contracts;
using MatteiGrid.Modules.Laboratory.Domain;
using MatteiGrid.Shared.Infrastructure.Persistence;
using MatteiGrid.Shared.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace MatteiGrid.Modules.Laboratory.Services;

public class LaboratoryService : ILaboratoryService
{
    private readonly AppDbContext _dbContext;
    private readonly ICurrentTenantContext _tenantContext;

    public LaboratoryService(AppDbContext dbContext, ICurrentTenantContext tenantContext)
    {
        _dbContext = dbContext;
        _tenantContext = tenantContext;
    }

    public async Task<LaboratoryOrderDto> CreateOrderForPrescriptionAsync(Guid prescriptionId, CancellationToken ct = default)
    {
        var order = new LaboratoryOrder
        {
            ClinicId = _tenantContext.ClinicId ?? Guid.Empty,
            PrescriptionId = prescriptionId,
            Status = LaboratoryOrderStatus.Received
        };

        _dbContext.Set<LaboratoryOrder>().Add(order);
        await _dbContext.SaveChangesAsync(ct);

        return MapDto(order);
    }

    public async Task<IReadOnlyList<LaboratoryOrderDto>> GetQueueAsync(string? statusFilter = null, CancellationToken ct = default)
    {
        var q = _dbContext.Set<LaboratoryOrder>().AsQueryable();

        if (!string.IsNullOrWhiteSpace(statusFilter))
        {
            q = q.Where(o => o.Status == statusFilter);
        }
        else
        {
            q = q.Where(o => o.Status != LaboratoryOrderStatus.Collected);
        }

        var orders = await q.OrderBy(o => o.CreatedAt).ToListAsync(ct);
        return orders.Select(MapDto).ToList();
    }

    public async Task<LaboratoryOrderDto> TransitionStatusAsync(Guid orderId, string targetStatus, Guid? staffId = null, CancellationToken ct = default)
    {
        var order = await _dbContext.Set<LaboratoryOrder>().FirstOrDefaultAsync(o => o.Id == orderId, ct)
            ?? throw new InvalidOperationException("Laboratory order not found.");

        if (order.Status == LaboratoryOrderStatus.Rejected)
            throw new InvalidOperationException("Rejected laboratory orders cannot be transitioned. A revised prescription must be issued.");

        order.Status = targetStatus;
        if (staffId.HasValue) order.PreparedByStaffId = staffId.Value;
        order.UpdatedAt = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync(ct);
        return MapDto(order);
    }

    public async Task<LaboratoryOrderDto> RejectOrderAsync(Guid orderId, string comment, Guid staffId, CancellationToken ct = default)
    {
        var order = await _dbContext.Set<LaboratoryOrder>().FirstOrDefaultAsync(o => o.Id == orderId, ct)
            ?? throw new InvalidOperationException("Laboratory order not found.");

        order.PreparedByStaffId = staffId;
        order.Reject(comment);

        await _dbContext.SaveChangesAsync(ct);
        return MapDto(order);
    }

    private static LaboratoryOrderDto MapDto(LaboratoryOrder o) =>
        new(o.Id, o.ClinicId, o.PrescriptionId, o.PreparedByStaffId, o.Status, o.RejectionComment, o.CreatedAt, o.UpdatedAt);
}