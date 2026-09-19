using MatteiGrid.Modules.Billing.Domain;
using MatteiGrid.Shared.Infrastructure.Persistence;
using MatteiGrid.Shared.Kernel;
using Microsoft.EntityFrameworkCore;

namespace MatteiGrid.Modules.Billing.Services;

public class PaymentLockChecker : IPaymentLockChecker
{
    private readonly AppDbContext _dbContext;

    public PaymentLockChecker(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> HasConfirmedPaymentForPrescriptionAsync(Guid prescriptionId, CancellationToken ct = default)
    {
        return await _dbContext.Set<Invoice>()
            .AnyAsync(i => i.PrescriptionId == prescriptionId && i.Payments.Any(p => p.Status == PaymentStatus.Confirmed), ct);
    }
}
