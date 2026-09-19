using MatteiGrid.Shared.Kernel;
using MatteiGrid.Shared.Infrastructure.Persistence;

namespace MatteiGrid.Shared.Infrastructure.Audit;

public class AuditLog : IClinicScopedEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ClinicId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }

    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public Guid? EntityId { get; set; }
    public string? UserId { get; set; }
    public string? Details { get; set; }
    public string? IpAddress { get; set; }
}

public interface IAuditService
{
    Task LogAsync(string action, string entityType, Guid? entityId, string? details = null, CancellationToken cancellationToken = default);
}

public class AuditService : IAuditService
{
    private readonly AppDbContext _dbContext;
    private readonly Tenancy.ICurrentTenantContext _tenantContext;

    public AuditService(AppDbContext dbContext, Tenancy.ICurrentTenantContext tenantContext)
    {
        _dbContext = dbContext;
        _tenantContext = tenantContext;
    }

    public async Task LogAsync(string action, string entityType, Guid? entityId, string? details = null, CancellationToken cancellationToken = default)
    {
        if (_tenantContext.ClinicId is null) return;

        _dbContext.AuditLogs.Add(new AuditLog
        {
            ClinicId = _tenantContext.ClinicId.Value,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            UserId = _tenantContext.UserId?.ToString(),
            Details = details,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = _tenantContext.UserId
        });
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
