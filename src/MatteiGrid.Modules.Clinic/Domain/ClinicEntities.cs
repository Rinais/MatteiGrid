using MatteiGrid.Shared.Kernel;

namespace MatteiGrid.Modules.Clinic.Domain;

public class Clinic : IAuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string? RazorpayLinkedAccountId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }

    public ICollection<Staff> StaffMembers { get; set; } = new List<Staff>();
    public ICollection<Role> Roles { get; set; } = new List<Role>();
}

public class Staff : ClinicScopedEntity
{
    public Guid IdentityUserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public Clinic? Clinic { get; set; }
    public ICollection<StaffRole> StaffRoles { get; set; } = new List<StaffRole>();
}

public class Role : ClinicScopedEntity
{
    public string Name { get; set; } = string.Empty;

    public Clinic? Clinic { get; set; }
    public ICollection<StaffRole> StaffRoles { get; set; } = new List<StaffRole>();
    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}

public class Permission
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}

public class StaffRole
{
    public Guid StaffId { get; set; }
    public Guid RoleId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Staff? Staff { get; set; }
    public Role? Role { get; set; }
}

public class RolePermission
{
    public Guid RoleId { get; set; }
    public Guid PermissionId { get; set; }

    public Role? Role { get; set; }
    public Permission? Permission { get; set; }
}
