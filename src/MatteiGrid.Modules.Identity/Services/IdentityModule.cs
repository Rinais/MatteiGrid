using MatteiGrid.Modules.Identity.Contracts;
using MatteiGrid.Modules.Identity.Domain;
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

    public IdentityModule(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole<Guid>> roleManager,
        AppDbContext dbContext,
        JwtTokenService jwtTokenService,
        ICurrentTenantContext tenantContext)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _dbContext = dbContext;
        _jwtTokenService = jwtTokenService;
        _tenantContext = tenantContext;
    }

    public async Task<LoginResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null || !user.IsActive) return null;
        if (!await _userManager.CheckPasswordAsync(user, request.Password)) return null;

        var roles = await _userManager.GetRolesAsync(user);
        var role = roles.FirstOrDefault() ?? "Receptionist";
        var permissions = await GetUserPermissionsAsync(user.Id, cancellationToken);

        var (accessToken, expiresAt) = _jwtTokenService.GenerateAccessToken(
            user.Id, user.Email!, user.ClinicId, permissions, role);

        var refreshToken = _jwtTokenService.GenerateRefreshToken();
        _dbContext.Set<RefreshToken>().Add(new RefreshToken
        {
            UserId = user.Id,
            ClinicId = user.ClinicId,
            TokenHash = _jwtTokenService.HashToken(refreshToken),
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(7)
        });
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new LoginResponse(accessToken, refreshToken, expiresAt, MapUser(user, role, permissions));
    }

    public async Task<LoginResponse?> RefreshAsync(RefreshRequest request, CancellationToken cancellationToken = default)
    {
        var hash = _jwtTokenService.HashToken(request.RefreshToken);
        var stored = await _dbContext.Set<RefreshToken>()
            .FirstOrDefaultAsync(t => t.TokenHash == hash && t.RevokedAt == null, cancellationToken);

        if (stored is null || stored.ExpiresAt < DateTimeOffset.UtcNow) return null;

        var user = await _userManager.FindByIdAsync(stored.UserId.ToString());
        if (user is null || !user.IsActive) return null;

        stored.RevokedAt = DateTimeOffset.UtcNow;

        var roles = await _userManager.GetRolesAsync(user);
        var role = roles.FirstOrDefault() ?? "Receptionist";
        var permissions = await GetUserPermissionsAsync(user.Id, cancellationToken);

        var (accessToken, expiresAt) = _jwtTokenService.GenerateAccessToken(
            user.Id, user.Email!, user.ClinicId, permissions, role);

        var newRefresh = _jwtTokenService.GenerateRefreshToken();
        stored.ReplacedByTokenHash = _jwtTokenService.HashToken(newRefresh);

        _dbContext.Set<RefreshToken>().Add(new RefreshToken
        {
            UserId = user.Id,
            ClinicId = user.ClinicId,
            TokenHash = _jwtTokenService.HashToken(newRefresh),
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(7)
        });
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new LoginResponse(accessToken, newRefresh, expiresAt, MapUser(user, role, permissions));
    }

    public async Task LogoutAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var hash = _jwtTokenService.HashToken(refreshToken);
        var stored = await _dbContext.Set<RefreshToken>().FirstOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);
        if (stored is not null)
        {
            stored.RevokedAt = DateTimeOffset.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<UserDto?> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null) return null;
        var roles = await _userManager.GetRolesAsync(user);
        var role = roles.FirstOrDefault() ?? "Receptionist";
        var permissions = await GetUserPermissionsAsync(userId, cancellationToken);
        return MapUser(user, role, permissions);
    }

    public async Task<UserDto> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        if (_tenantContext.ClinicId is null)
            throw new InvalidOperationException("Tenant context required.");

        var user = new ApplicationUser
        {
            Email = request.Email,
            UserName = request.Email,
            FullName = request.FullName,
            ClinicId = _tenantContext.ClinicId.Value,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));

        await _userManager.AddToRoleAsync(user, request.Role);
        var permissions = await GetUserPermissionsAsync(user.Id, cancellationToken);
        return MapUser(user, request.Role, permissions);
    }

    public async Task<IReadOnlyList<UserDto>> GetUsersAsync(CancellationToken cancellationToken = default)
    {
        var users = await _userManager.Users.Where(u => u.IsActive).ToListAsync(cancellationToken);
        var result = new List<UserDto>();
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault() ?? "Receptionist";
            var permissions = await GetUserPermissionsAsync(user.Id, cancellationToken);
            result.Add(MapUser(user, role, permissions));
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
    }

    public async Task<IReadOnlyList<string>> GetUserPermissionsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null) return Array.Empty<string>();

        var roles = await _userManager.GetRolesAsync(user);
        var roleIds = await _dbContext.Set<IdentityRole<Guid>>()
            .Where(r => roles.Contains(r.Name!))
            .Select(r => r.Id)
            .ToListAsync(cancellationToken);

        return await _dbContext.Set<RolePermission>()
            .Where(rp => roleIds.Contains(rp.RoleId))
            .Join(_dbContext.Set<Permission>(), rp => rp.PermissionId, p => p.Id, (_, p) => p.Name)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    private static UserDto MapUser(ApplicationUser user, string role, IReadOnlyList<string> permissions) =>
        new(user.Id, user.Email!, user.FullName, role, permissions);
}
