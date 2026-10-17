namespace MatteiGrid.Shared.Infrastructure.Tenancy;

public interface ICurrentTenantContext
{
    Guid? ClinicId { get; }
    Guid? UserId { get; }
    string? Role { get; }
    bool IsAuthenticated { get; }
    bool IsBreakGlassSession { get; }
    string? BreakGlassReason { get; }
    IReadOnlyList<string> Permissions { get; }
    bool HasPermission(string permission);
    void SetTenant(Guid clinicId, Guid userId, IReadOnlyList<string> permissions);
    void SetTenant(Guid clinicId, Guid userId, string role, IReadOnlyList<string> permissions, bool isBreakGlass = false, string? breakGlassReason = null);
}

public sealed class CurrentTenantContext : ICurrentTenantContext
{
    public Guid? ClinicId { get; private set; }
    public Guid? UserId { get; private set; }
    public string? Role { get; private set; }
    public bool IsAuthenticated => UserId.HasValue && UserId.Value != Guid.Empty;
    public bool IsBreakGlassSession { get; private set; }
    public string? BreakGlassReason { get; private set; }
    public IReadOnlyList<string> Permissions { get; private set; } = Array.Empty<string>();

    public bool HasPermission(string permission) =>
        Permissions.Contains(permission, StringComparer.OrdinalIgnoreCase);

    public void SetTenant(Guid clinicId, Guid userId, IReadOnlyList<string> permissions)
    {
        SetTenant(clinicId, userId, string.Empty, permissions, false, null);
    }

    public void SetTenant(Guid clinicId, Guid userId, string role, IReadOnlyList<string> permissions, bool isBreakGlass = false, string? breakGlassReason = null)
    {
        ClinicId = clinicId;
        UserId = userId;
        Role = role;
        Permissions = permissions;
        IsBreakGlassSession = isBreakGlass;
        BreakGlassReason = breakGlassReason;
    }
}
