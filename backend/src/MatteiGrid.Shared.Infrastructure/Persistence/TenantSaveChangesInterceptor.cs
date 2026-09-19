using MatteiGrid.Shared.Kernel;
using MatteiGrid.Shared.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace MatteiGrid.Shared.Infrastructure.Persistence;

public sealed class TenantSaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentTenantContext _tenantContext;

    public TenantSaveChangesInterceptor(ICurrentTenantContext tenantContext)
    {
        _tenantContext = tenantContext;
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        StampTenant(eventData.Context);
        StampAudit(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        StampTenant(eventData.Context);
        StampAudit(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void StampTenant(DbContext? context)
    {
        if (context is null || _tenantContext.ClinicId is null) return;

        foreach (var entry in context.ChangeTracker.Entries<IClinicScopedEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.ClinicId = _tenantContext.ClinicId.Value;
            }
            else if (entry.State == EntityState.Modified && entry.Entity.ClinicId != _tenantContext.ClinicId.Value)
            {
                throw new InvalidOperationException("Cross-tenant write attempt detected.");
            }
        }
    }

    private void StampAudit(DbContext? context)
    {
        if (context is null) return;
        var now = DateTimeOffset.UtcNow;
        var userId = _tenantContext.UserId;

        foreach (var entry in context.ChangeTracker.Entries<IAuditableEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
                entry.Entity.CreatedBy = userId;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
                entry.Entity.UpdatedBy = userId;
            }
        }
    }
}
