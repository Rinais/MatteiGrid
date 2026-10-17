using MatteiGrid.Shared.Kernel;
using MatteiGrid.Shared.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MatteiGrid.Shared.Infrastructure.Audit;

public static class AuditEventTypes
{
    public const string AuthLogin = "Auth.Login";
    public const string AuthLoginFailed = "Auth.LoginFailed";
    public const string AuthLockout = "Auth.Lockout";
    public const string AuthTokenRefresh = "Auth.TokenRefresh";
    public const string AuthTokenRevokedReuse = "Auth.TokenRevokedReuse";
    public const string AuthLogout = "Auth.Logout";
    
    public const string PhiRead = "Phi.Read";
    public const string PhiConsultationAccess = "Phi.ConsultationAccess";
    public const string PhiPrescriptionView = "Phi.PrescriptionView";

    public const string RoleChanged = "Role.Changed";
    public const string UserCreated = "User.Created";
    public const string UserDisabled = "User.Disabled";

    public const string PiiEraseRequested = "Pii.EraseRequested";
    public const string PiiErased = "Pii.Erased";

    public const string BillingRefund = "Billing.Refund";
    public const string BillingManualPayment = "Billing.ManualPayment";

    public const string SuperAdminBreakGlass = "SuperAdmin.BreakGlass";
}

public class AuditLog : IClinicScopedEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ClinicId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }

    public string EventType { get; set; } = "General";
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public Guid? EntityId { get; set; }
    public string? UserId { get; set; }
    public string? UserEmail { get; set; }
    public string? Details { get; set; }
    public string? MetadataJson { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public bool IsBreakGlass { get; set; }
}

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_logs");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Action).HasMaxLength(128).IsRequired();
        builder.Property(a => a.EventType).HasMaxLength(64).IsRequired();
        builder.Property(a => a.EntityType).HasMaxLength(128).IsRequired();
        builder.Property(a => a.UserId).HasMaxLength(128);
        builder.Property(a => a.UserEmail).HasMaxLength(256);
        builder.Property(a => a.IpAddress).HasMaxLength(64);
        builder.Property(a => a.UserAgent).HasMaxLength(512);

        builder.HasIndex(a => new { a.ClinicId, a.CreatedAt });
        builder.HasIndex(a => a.EventType);
    }
}

public interface IAuditService
{
    Task LogAsync(string action, string entityType, Guid? entityId, string? details = null, CancellationToken cancellationToken = default);
    Task LogSecurityEventAsync(string eventType, string action, string entityType, Guid? entityId, string? details = null, string? metadataJson = null, Guid? clinicIdOverride = null, string? userIdOverride = null, string? userEmailOverride = null, CancellationToken cancellationToken = default);
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

    public Task LogAsync(string action, string entityType, Guid? entityId, string? details = null, CancellationToken cancellationToken = default)
    {
        return LogSecurityEventAsync("General", action, entityType, entityId, details, null, null, null, null, cancellationToken);
    }

    public async Task LogSecurityEventAsync(string eventType, string action, string entityType, Guid? entityId, string? details = null, string? metadataJson = null, Guid? clinicIdOverride = null, string? userIdOverride = null, string? userEmailOverride = null, CancellationToken cancellationToken = default)
    {
        var clinicId = clinicIdOverride ?? _tenantContext.ClinicId;
        if (clinicId is null || clinicId == Guid.Empty) return;

        var log = new AuditLog
        {
            ClinicId = clinicId.Value,
            EventType = eventType,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            UserId = userIdOverride ?? _tenantContext.UserId?.ToString(),
            UserEmail = userEmailOverride,
            Details = details,
            MetadataJson = metadataJson,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = _tenantContext.UserId,
            IsBreakGlass = _tenantContext.IsBreakGlassSession
        };

        _dbContext.AuditLogs.Add(log);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
