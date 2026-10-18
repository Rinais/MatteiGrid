using MatteiGrid.Modules.Identity.Contracts;
using MatteiGrid.Modules.Identity.Domain;
using MatteiGrid.Shared.Infrastructure.Audit;
using MatteiGrid.Shared.Infrastructure.Persistence;
using MatteiGrid.Shared.Infrastructure.Tenancy;
using MatteiGrid.Shared.Kernel;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace MatteiGrid.Modules.Identity.Services;

public class IdentityModule : IIdentityModule
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;
    private readonly AppDbContext _dbContext;
    private readonly JwtTokenService _jwtTokenService;
    private readonly ICurrentTenantContext _tenantContext;
    private readonly IAuditService _auditService;

    public IdentityModule(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole<Guid>> roleManager,
        AppDbContext dbContext,
        JwtTokenService jwtTokenService,
        ICurrentTenantContext tenantContext,
        IAuditService auditService)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _dbContext = dbContext;
        _jwtTokenService = jwtTokenService;
        _tenantContext = tenantContext;
        _auditService = auditService;
    }

    public async Task<LoginResponse?> LoginAsync(LoginRequest request, string? clientIp = null, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null || !user.IsActive)
        {
            await _auditService.LogSecurityEventAsync(
                AuditEventTypes.AuthLoginFailed,
                "LoginFailed",
                "User",
                null,
                $"Invalid credentials for email: {request.Email}",
                null,
                null,
                null,
                request.Email,
                cancellationToken);
            return null;
        }

        // Check account lockout
        if (await _userManager.IsLockedOutAsync(user))
        {
            await _auditService.LogSecurityEventAsync(
                AuditEventTypes.AuthLockout,
                "AccountLockedOut",
                "User",
                user.Id,
                $"Account locked out until {user.LockoutEnd}",
                null,
                user.ClinicId,
                user.Id.ToString(),
                user.Email,
                cancellationToken);
            return null;
        }

        // Verify password
        if (!await _userManager.CheckPasswordAsync(user, request.Password))
        {
            await _userManager.AccessFailedAsync(user);
            await _auditService.LogSecurityEventAsync(
                AuditEventTypes.AuthLoginFailed,
                "LoginFailed",
                "User",
                user.Id,
                "Incorrect password entered",
                null,
                user.ClinicId,
                user.Id.ToString(),
                user.Email,
                cancellationToken);
            return null;
        }

        // Reset failed count on successful authentication
        await _userManager.ResetAccessFailedCountAsync(user);

        // Fetch active clinic memberships
        var memberships = await _dbContext.Set<UserClinicMembership>()
            .Where(m => m.UserId == user.Id && m.IsActive)
            .ToListAsync(cancellationToken);

        // Fallback: If no explicit UserClinicMembership exists, use user.ClinicId and assigned Identity roles
        if (memberships.Count == 0 && user.ClinicId != Guid.Empty)
        {
            var userRoles = await _userManager.GetRolesAsync(user);
            var primaryRole = userRoles.FirstOrDefault() ?? Roles.Receptionist;
            memberships.Add(new UserClinicMembership
            {
                UserId = user.Id,
                ClinicId = user.ClinicId,
                ClinicName = "Primary Clinic",
                Role = primaryRole,
                IsActive = true
            });
        }

        // Check if tenant selection is required (Decision 1)
        if (memberships.Count > 1 && !request.ClinicId.HasValue)
        {
            var selectionToken = _jwtTokenService.GenerateTenantSelectionToken(user.Id, user.Email!);
            return new LoginResponse(
                AccessToken: null,
                RefreshToken: null,
                ExpiresAt: null,
                User: null,
                RequiresTenantSelection: true,
                TenantSelectionToken: selectionToken,
                AvailableClinics: memberships.Select(m => new ClinicMembershipDto(m.ClinicId, m.ClinicName, m.Role)).ToList());
        }

        // Single clinic or explicitly selected clinic
        var selectedMembership = request.ClinicId.HasValue
            ? memberships.FirstOrDefault(m => m.ClinicId == request.ClinicId.Value)
            : memberships.FirstOrDefault();

        if (selectedMembership is null)
        {
            return null;
        }

        var clinicId = selectedMembership.ClinicId;
        var role = selectedMembership.Role;
        var permissions = ResolvePermissionsForRole(role);

        var (accessToken, expiresAt) = _jwtTokenService.GenerateAccessToken(
            user.Id, user.Email!, clinicId, role, user.TokenVersion);

        var rawRefreshToken = _jwtTokenService.GenerateRefreshToken();
        var familyId = Guid.NewGuid();

        _dbContext.Set<RefreshToken>().Add(new RefreshToken
        {
            UserId = user.Id,
            ClinicId = clinicId,
            FamilyId = familyId,
            TokenHash = _jwtTokenService.HashToken(rawRefreshToken),
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(7),
            CreatedByIp = clientIp
        });
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditService.LogSecurityEventAsync(
            AuditEventTypes.AuthLogin,
            "LoginSuccess",
            "User",
            user.Id,
            $"Successful login for {user.Email} at clinic {clinicId} (Role: {role})",
            null,
            clinicId,
            user.Id.ToString(),
            user.Email,
            cancellationToken);

        return new LoginResponse(
            accessToken,
            rawRefreshToken,
            expiresAt,
            new UserDto(user.Id, user.Email!, user.FullName, role, clinicId, permissions));
    }

    public async Task<LoginResponse?> SelectTenantAsync(SelectTenantRequest request, string? clientIp = null, CancellationToken cancellationToken = default)
    {
        var validated = _jwtTokenService.ValidateTenantSelectionToken(request.TenantSelectionToken);
        if (validated is null) return null;

        var (userId, email) = validated.Value;
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null || !user.IsActive) return null;

        var membership = await _dbContext.Set<UserClinicMembership>()
            .FirstOrDefaultAsync(m => m.UserId == userId && m.ClinicId == request.ClinicId && m.IsActive, cancellationToken);

        if (membership is null) return null;

        var role = membership.Role;
        var permissions = ResolvePermissionsForRole(role);

        var (accessToken, expiresAt) = _jwtTokenService.GenerateAccessToken(
            user.Id, user.Email!, membership.ClinicId, role, user.TokenVersion);

        var rawRefreshToken = _jwtTokenService.GenerateRefreshToken();
        var familyId = Guid.NewGuid();

        _dbContext.Set<RefreshToken>().Add(new RefreshToken
        {
            UserId = user.Id,
            ClinicId = membership.ClinicId,
            FamilyId = familyId,
            TokenHash = _jwtTokenService.HashToken(rawRefreshToken),
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(7),
            CreatedByIp = clientIp
        });
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditService.LogSecurityEventAsync(
            AuditEventTypes.AuthLogin,
            "TenantSelected",
            "User",
            user.Id,
            $"User {user.Email} selected clinic {membership.ClinicId} with role {role}",
            null,
            membership.ClinicId,
            user.Id.ToString(),
            user.Email,
            cancellationToken);

        return new LoginResponse(
            accessToken,
            rawRefreshToken,
            expiresAt,
            new UserDto(user.Id, user.Email!, user.FullName, role, membership.ClinicId, permissions));
    }

    public async Task<LoginResponse?> RefreshAsync(RefreshRequest request, string? clientIp = null, CancellationToken cancellationToken = default)
    {
        var hash = _jwtTokenService.HashToken(request.RefreshToken);
        var stored = await _dbContext.Set<RefreshToken>()
            .FirstOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);

        if (stored is null) return null;

        // AUTH-04: Refresh Token Reuse Detection
        // If an already-revoked token is presented, this indicates token theft!
        // Immediately revoke the entire token family.
        if (stored.RevokedAt != null)
        {
            var familyTokens = await _dbContext.Set<RefreshToken>()
                .Where(t => t.FamilyId == stored.FamilyId && t.RevokedAt == null)
                .ToListAsync(cancellationToken);

            foreach (var token in familyTokens)
            {
                token.RevokedAt = DateTimeOffset.UtcNow;
            }
            await _dbContext.SaveChangesAsync(cancellationToken);

            await _auditService.LogSecurityEventAsync(
                AuditEventTypes.AuthTokenRevokedReuse,
                "RefreshTokenReplayDetected",
                "RefreshToken",
                stored.Id,
                $"CRITICAL: Replayed revoked refresh token detected! Family {stored.FamilyId} completely revoked.",
                null,
                stored.ClinicId,
                stored.UserId.ToString(),
                null,
                cancellationToken);

            return null;
        }

        // Expired token check
        if (stored.ExpiresAt < DateTimeOffset.UtcNow)
        {
            stored.RevokedAt = DateTimeOffset.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
            return null;
        }

        var user = await _userManager.FindByIdAsync(stored.UserId.ToString());
        if (user is null || !user.IsActive) return null;

        // Revoke current token as part of standard rotation
        stored.RevokedAt = DateTimeOffset.UtcNow;

        // Determine user's current role at this clinic
        var membership = await _dbContext.Set<UserClinicMembership>()
            .FirstOrDefaultAsync(m => m.UserId == user.Id && m.ClinicId == stored.ClinicId && m.IsActive, cancellationToken);

        string role;
        if (membership != null)
        {
            role = membership.Role;
        }
        else
        {
            var roles = await _userManager.GetRolesAsync(user);
            role = roles.FirstOrDefault() ?? Roles.Receptionist;
        }

        var permissions = ResolvePermissionsForRole(role);

        var (accessToken, expiresAt) = _jwtTokenService.GenerateAccessToken(
            user.Id, user.Email!, stored.ClinicId, role, user.TokenVersion);

        var newRawRefreshToken = _jwtTokenService.GenerateRefreshToken();
        var newHash = _jwtTokenService.HashToken(newRawRefreshToken);
        stored.ReplacedByTokenHash = newHash;

        _dbContext.Set<RefreshToken>().Add(new RefreshToken
        {
            UserId = user.Id,
            ClinicId = stored.ClinicId,
            FamilyId = stored.FamilyId, // Maintain family ID for rotation chain
            TokenHash = newHash,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(7),
            CreatedByIp = clientIp
        });
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditService.LogSecurityEventAsync(
            AuditEventTypes.AuthTokenRefresh,
            "TokenRefreshed",
            "RefreshToken",
            stored.Id,
            $"Refresh token rotated for user {user.Email} at clinic {stored.ClinicId}",
            null,
            stored.ClinicId,
            user.Id.ToString(),
            user.Email,
            cancellationToken);

        return new LoginResponse(
            accessToken,
            newRawRefreshToken,
            expiresAt,
            new UserDto(user.Id, user.Email!, user.FullName, role, stored.ClinicId, permissions));
    }

    public async Task LogoutAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var hash = _jwtTokenService.HashToken(refreshToken);
        var stored = await _dbContext.Set<RefreshToken>().FirstOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);
        if (stored is not null && stored.RevokedAt == null)
        {
            stored.RevokedAt = DateTimeOffset.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);

            await _auditService.LogSecurityEventAsync(
                AuditEventTypes.AuthLogout,
                "Logout",
                "RefreshToken",
                stored.Id,
                $"User {stored.UserId} logged out",
                null,
                stored.ClinicId,
                stored.UserId.ToString(),
                null,
                cancellationToken);
        }
    }

    public async Task<UserDto?> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null || !user.IsActive) return null;

        var clinicId = _tenantContext.ClinicId ?? user.ClinicId;
        var membership = await _dbContext.Set<UserClinicMembership>()
            .FirstOrDefaultAsync(m => m.UserId == userId && m.ClinicId == clinicId && m.IsActive, cancellationToken);

        string role;
        if (membership != null)
        {
            role = membership.Role;
        }
        else
        {
            var roles = await _userManager.GetRolesAsync(user);
            role = roles.FirstOrDefault() ?? Roles.Receptionist;
        }

        var permissions = ResolvePermissionsForRole(role);
        return new UserDto(user.Id, user.Email!, user.FullName, role, clinicId, permissions);
    }

    public async Task<UserDto> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        var clinicId = request.ClinicId ?? _tenantContext.ClinicId;
        if (clinicId is null || clinicId == Guid.Empty)
            throw new InvalidOperationException("Tenant context or ClinicId required.");

        var user = new ApplicationUser
        {
            Email = request.Email,
            UserName = request.Email,
            FullName = request.FullName,
            ClinicId = clinicId.Value,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));

        await _userManager.AddToRoleAsync(user, request.Role);

        // Add UserClinicMembership
        _dbContext.Set<UserClinicMembership>().Add(new UserClinicMembership
        {
            UserId = user.Id,
            ClinicId = clinicId.Value,
            ClinicName = "Clinic",
            Role = request.Role,
            IsActive = true
        });
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditService.LogSecurityEventAsync(
            AuditEventTypes.UserCreated,
            "UserCreated",
            "User",
            user.Id,
            $"Created user {user.Email} with role {request.Role} for clinic {clinicId.Value}",
            null,
            clinicId.Value,
            user.Id.ToString(),
            user.Email,
            cancellationToken);

        var permissions = ResolvePermissionsForRole(request.Role);
        return new UserDto(user.Id, user.Email!, user.FullName, request.Role, clinicId.Value, permissions);
    }

    public async Task<IReadOnlyList<UserDto>> GetUsersAsync(CancellationToken cancellationToken = default)
    {
        var currentClinicId = _tenantContext.ClinicId;
        var users = await _userManager.Users.Where(u => u.IsActive).ToListAsync(cancellationToken);
        var result = new List<UserDto>();

        foreach (var user in users)
        {
            var targetClinicId = currentClinicId ?? user.ClinicId;
            var membership = await _dbContext.Set<UserClinicMembership>()
                .FirstOrDefaultAsync(m => m.UserId == user.Id && m.ClinicId == targetClinicId && m.IsActive, cancellationToken);

            string role;
            if (membership != null)
            {
                role = membership.Role;
            }
            else
            {
                var roles = await _userManager.GetRolesAsync(user);
                role = roles.FirstOrDefault() ?? Roles.Receptionist;
            }

            var permissions = ResolvePermissionsForRole(role);
            result.Add(new UserDto(user.Id, user.Email!, user.FullName, role, targetClinicId, permissions));
        }

        return result;
    }

    public async Task AssignRoleAsync(Guid userId, string role, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString())
            ?? throw new InvalidOperationException("User not found.");

        var currentRoles = await _userManager.GetRolesAsync(user);
        await _userManager.RemoveFromRolesAsync(user, currentRoles);
        await _userManager.AddToRoleAsync(user, role);

        // Invalidate current tokens by bumping TokenVersion
        user.TokenVersion++;
        await _userManager.UpdateAsync(user);

        // Update active clinic membership
        var clinicId = _tenantContext.ClinicId ?? user.ClinicId;
        var membership = await _dbContext.Set<UserClinicMembership>()
            .FirstOrDefaultAsync(m => m.UserId == userId && m.ClinicId == clinicId, cancellationToken);

        if (membership != null)
        {
            membership.Role = role;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        await _auditService.LogSecurityEventAsync(
            AuditEventTypes.RoleChanged,
            "RoleAssigned",
            "User",
            user.Id,
            $"Assigned role {role} to user {user.Email}. Incremented TokenVersion to {user.TokenVersion}.",
            null,
            clinicId,
            user.Id.ToString(),
            user.Email,
            cancellationToken);
    }

    public async Task<IReadOnlyList<string>> GetUserPermissionsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null) return Array.Empty<string>();

        var roles = await _userManager.GetRolesAsync(user);
        var primaryRole = roles.FirstOrDefault() ?? Roles.Receptionist;
        return ResolvePermissionsForRole(primaryRole);
    }

    public async Task<BreakGlassResponse> InitiateBreakGlassAsync(
        Guid superAdminUserId,
        BreakGlassRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.DurationMinutes <= 0 || request.DurationMinutes > 120)
            throw new ArgumentOutOfRangeException(nameof(request.DurationMinutes), "Break-glass duration must be between 1 and 120 minutes.");

        if (string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Trim().Length < 20)
            throw new ArgumentException("Break-glass justification must be at least 20 characters.", nameof(request.Reason));

        var superAdmin = await _userManager.FindByIdAsync(superAdminUserId.ToString())
            ?? throw new InvalidOperationException("SuperAdmin user not found.");

        var roles = await _userManager.GetRolesAsync(superAdmin);
        if (!roles.Contains(Roles.SuperAdmin))
            throw new UnauthorizedAccessException("Only SuperAdmin users can initiate break-glass emergency access.");

        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(request.DurationMinutes);
        var session = new BreakGlassSession
        {
            ClinicId = request.ClinicId,
            SuperAdminUserId = superAdminUserId,
            TicketNumber = request.TicketNumber,
            Reason = request.Reason,
            StartedAt = DateTimeOffset.UtcNow,
            ExpiresAt = expiresAt
        };

        _dbContext.Set<BreakGlassSession>().Add(session);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Generate emergency token with break-glass claim bound to target clinic
        var (accessToken, _) = _jwtTokenService.GenerateAccessToken(
            superAdmin.Id,
            superAdmin.Email!,
            request.ClinicId,
            Roles.SuperAdmin,
            superAdmin.TokenVersion,
            isBreakGlass: true,
            breakGlassReason: request.Reason);

        await _auditService.LogSecurityEventAsync(
            AuditEventTypes.SuperAdminBreakGlass,
            "BreakGlassInitiated",
            "BreakGlassSession",
            session.Id,
            $"EMERGENCY BREAK-GLASS: SuperAdmin {superAdmin.Email} initiated access to clinic {request.ClinicId}. Ticket: {request.TicketNumber}. Reason: {request.Reason}",
            null,
            request.ClinicId,
            superAdmin.Id.ToString(),
            superAdmin.Email,
            cancellationToken);

        return new BreakGlassResponse(accessToken, expiresAt, session.Id, request.ClinicId);
    }

    public async Task<Result> AdminResetPasswordAsync(Guid userId, string newPassword, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return Result.Failure("User not found.");
        }

        await _userManager.RemovePasswordAsync(user);

        var addRes = await _userManager.AddPasswordAsync(user, newPassword);
        if (!addRes.Succeeded)
        {
            var errorMsg = string.Join("; ", addRes.Errors.Select(e => e.Description));
            return Result.Failure(errorMsg);
        }

        await _userManager.ResetAccessFailedCountAsync(user);
        await _userManager.SetLockoutEndDateAsync(user, null);
        await _userManager.UpdateSecurityStampAsync(user);

        await _auditService.LogSecurityEventAsync(
            "User.PasswordReset",
            "AdminResetPassword",
            "User",
            user.Id,
            $"Administrator reset password for user: {user.Email}",
            null,
            user.ClinicId,
            user.Id.ToString(),
            user.Email,
            cancellationToken);

        return Result.Success();
    }

    private static IReadOnlyList<string> ResolvePermissionsForRole(string role)
    {
        if (Permissions.RolePermissions.TryGetValue(role, out var permissions))
        {
            return permissions;
        }

        return Array.Empty<string>();
    }
}
