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
        if (context is null) return;

        foreach (var entry in context.ChangeTracker.Entries())
        {
            // AUTH-06: Append-only enforcement for audit logs
            if (entry.Entity is Audit.AuditLog && (entry.State == EntityState.Modified || entry.State == EntityState.Deleted))
            {
                throw new InvalidOperationException("Audit logs are append-only and cannot be modified or deleted.");
            }

            // AUTH-02: Tenant isolation checks on write, modify, delete
            if (entry.Entity is IClinicScopedEntity scopedEntity)
            {
                if (entry.State == EntityState.Added)
                {
                    if (scopedEntity.ClinicId == Guid.Empty)
                    {
                        if (_tenantContext.ClinicId.HasValue)
                        {
                            scopedEntity.ClinicId = _tenantContext.ClinicId.Value;
                        }
                    }
                    else if (_tenantContext.ClinicId.HasValue && scopedEntity.ClinicId != _tenantContext.ClinicId.Value)
                    {
                        throw new InvalidOperationException(
                            $"Cross-tenant write attempt detected: entity ClinicId '{scopedEntity.ClinicId}' does not match context '{_tenantContext.ClinicId}'.");
                    }
                }
                else if (entry.State == EntityState.Modified || entry.State == EntityState.Deleted)
                {
                    if (_tenantContext.ClinicId.HasValue && scopedEntity.ClinicId != _tenantContext.ClinicId.Value)
                    {
                        throw new InvalidOperationException(
                            $"Cross-tenant mutation attempt detected: entity ClinicId '{scopedEntity.ClinicId}' does not match context '{_tenantContext.ClinicId}'.");
                    }
                }
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
