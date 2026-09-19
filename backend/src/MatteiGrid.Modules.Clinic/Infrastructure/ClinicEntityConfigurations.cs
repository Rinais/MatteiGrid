using MatteiGrid.Modules.Clinic.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MatteiGrid.Modules.Clinic.Infrastructure;

public class ClinicConfiguration : IEntityTypeConfiguration<Domain.Clinic>
{
    public void Configure(EntityTypeBuilder<Domain.Clinic> builder)
    {
        builder.ToTable("Clinics");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Name).IsRequired().HasMaxLength(200);
        builder.Property(c => c.RazorpayLinkedAccountId).HasMaxLength(100);
    }
}

public class StaffConfiguration : IEntityTypeConfiguration<Staff>
{
    public void Configure(EntityTypeBuilder<Staff> builder)
    {
        builder.ToTable("Staff");
        builder.HasKey(s => s.Id);
        builder.HasIndex(s => new { s.ClinicId, s.Id }).IsUnique();
        builder.Property(s => s.Name).IsRequired().HasMaxLength(200);
        builder.Property(s => s.Phone).IsRequired().HasMaxLength(20);
        builder.HasIndex(s => new { s.ClinicId, s.IdentityUserId }).IsUnique();

        builder.HasOne(s => s.Clinic)
            .WithMany(c => c.StaffMembers)
            .HasForeignKey(s => s.ClinicId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("Roles");
        builder.HasKey(r => r.Id);
        builder.HasIndex(r => new { r.ClinicId, r.Id }).IsUnique();
        builder.Property(r => r.Name).IsRequired().HasMaxLength(100);

        builder.HasOne(r => r.Clinic)
            .WithMany(c => c.Roles)
            .HasForeignKey(r => r.ClinicId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("Permissions");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Code).IsRequired().HasMaxLength(100);
        builder.HasIndex(p => p.Code).IsUnique();
        builder.Property(p => p.Description).HasMaxLength(500);
    }
}

public class StaffRoleConfiguration : IEntityTypeConfiguration<StaffRole>
{
    public void Configure(EntityTypeBuilder<StaffRole> builder)
    {
        builder.ToTable("StaffRoles");
        builder.HasKey(sr => new { sr.StaffId, sr.RoleId });

        builder.HasOne(sr => sr.Staff)
            .WithMany(s => s.StaffRoles)
            .HasForeignKey(sr => sr.StaffId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(sr => sr.Role)
            .WithMany(r => r.StaffRoles)
            .HasForeignKey(sr => sr.RoleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.ToTable("RolePermissions");
        builder.HasKey(rp => new { rp.RoleId, rp.PermissionId });

        builder.HasOne(rp => rp.Role)
            .WithMany(r => r.RolePermissions)
            .HasForeignKey(rp => rp.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(rp => rp.Permission)
            .WithMany(p => p.RolePermissions)
            .HasForeignKey(rp => rp.PermissionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
