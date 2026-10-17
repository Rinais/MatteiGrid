using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MatteiGrid.Modules.Identity.Domain;

public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.ToTable("AspNetUsers");
        builder.HasKey(u => u.Id);
        builder.HasIndex(u => u.NormalizedUserName).HasDatabaseName("UserNameIndex").IsUnique();
        builder.HasIndex(u => u.NormalizedEmail).HasDatabaseName("EmailIndex");
        builder.Property(u => u.ConcurrencyStamp).IsConcurrencyToken();
        builder.Property(u => u.UserName).HasMaxLength(256);
        builder.Property(u => u.NormalizedUserName).HasMaxLength(256);
        builder.Property(u => u.Email).HasMaxLength(256);
        builder.Property(u => u.NormalizedEmail).HasMaxLength(256);
    }
}

public class IdentityRoleConfiguration : IEntityTypeConfiguration<IdentityRole<Guid>>
{
    public void Configure(EntityTypeBuilder<IdentityRole<Guid>> builder)
    {
        builder.ToTable("AspNetRoles");
        builder.HasKey(r => r.Id);
        builder.HasIndex(r => r.NormalizedName).HasDatabaseName("RoleNameIndex").IsUnique();
        builder.Property(r => r.ConcurrencyStamp).IsConcurrencyToken();
        builder.Property(r => r.Name).HasMaxLength(256);
        builder.Property(r => r.NormalizedName).HasMaxLength(256);
    }
}

public class IdentityUserRoleConfiguration : IEntityTypeConfiguration<IdentityUserRole<Guid>>
{
    public void Configure(EntityTypeBuilder<IdentityUserRole<Guid>> builder)
    {
        builder.ToTable("AspNetUserRoles");
        builder.HasKey(r => new { r.UserId, r.RoleId });
    }
}

public class IdentityUserClaimConfiguration : IEntityTypeConfiguration<IdentityUserClaim<Guid>>
{
    public void Configure(EntityTypeBuilder<IdentityUserClaim<Guid>> builder)
    {
        builder.ToTable("AspNetUserClaims");
        builder.HasKey(uc => uc.Id);
    }
}

public class IdentityRoleClaimConfiguration : IEntityTypeConfiguration<IdentityRoleClaim<Guid>>
{
    public void Configure(EntityTypeBuilder<IdentityRoleClaim<Guid>> builder)
    {
        builder.ToTable("AspNetRoleClaims");
        builder.HasKey(rc => rc.Id);
    }
}

public class IdentityUserLoginConfiguration : IEntityTypeConfiguration<IdentityUserLogin<Guid>>
{
    public void Configure(EntityTypeBuilder<IdentityUserLogin<Guid>> builder)
    {
        builder.ToTable("AspNetUserLogins");
        builder.HasKey(l => new { l.LoginProvider, l.ProviderKey });
    }
}

public class IdentityUserTokenConfiguration : IEntityTypeConfiguration<IdentityUserToken<Guid>>
{
    public void Configure(EntityTypeBuilder<IdentityUserToken<Guid>> builder)
    {
        builder.ToTable("AspNetUserTokens");
        builder.HasKey(t => new { t.UserId, t.LoginProvider, t.Name });
    }
}

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.TokenHash).HasMaxLength(128).IsRequired();
        builder.Property(r => r.ReplacedByTokenHash).HasMaxLength(128);
        builder.Property(r => r.CreatedByIp).HasMaxLength(64);
        builder.HasIndex(r => r.TokenHash);
        builder.HasIndex(r => r.FamilyId);
        builder.HasIndex(r => new { r.UserId, r.ClinicId });
    }
}

public class UserClinicMembershipConfiguration : IEntityTypeConfiguration<UserClinicMembership>
{
    public void Configure(EntityTypeBuilder<UserClinicMembership> builder)
    {
        builder.ToTable("user_clinic_memberships");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.ClinicName).HasMaxLength(256).IsRequired();
        builder.Property(m => m.Role).HasMaxLength(64).IsRequired();
        builder.HasIndex(m => new { m.UserId, m.ClinicId }).IsUnique();
    }
}

public class BreakGlassSessionConfiguration : IEntityTypeConfiguration<BreakGlassSession>
{
    public void Configure(EntityTypeBuilder<BreakGlassSession> builder)
    {
        builder.ToTable("break_glass_sessions");
        builder.HasKey(b => b.Id);
        builder.Property(b => b.TicketNumber).HasMaxLength(64).IsRequired();
        builder.Property(b => b.Reason).HasMaxLength(1024).IsRequired();
        builder.HasIndex(b => new { b.ClinicId, b.SuperAdminUserId });
    }
}
