namespace MatteiGrid.Shared.Infrastructure.Tenancy;

public interface ICurrentTenantContext
{
    Guid? ClinicId { get; }
    Guid? UserId { get; }
    IReadOnlyList<string> Permissions { get; }
    bool HasPermission(string permission);
    void SetTenant(Guid clinicId, Guid userId, IReadOnlyList<string> permissions);
}

public sealed class CurrentTenantContext : ICurrentTenantContext
{
    public Guid? ClinicId { get; private set; }
    public Guid? UserId { get; private set; }
    public IReadOnlyList<string> Permissions { get; private set; } = Array.Empty<string>();

    public bool HasPermission(string permission) =>
        Permissions.Contains(permission, StringComparer.OrdinalIgnoreCase);

    public void SetTenant(Guid clinicId, Guid userId, IReadOnlyList<string> permissions)
    {
        ClinicId = clinicId;
        UserId = userId;
        Permissions = permissions;
    }
}
