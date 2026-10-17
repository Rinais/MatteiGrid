namespace MatteiGrid.Modules.Identity.Contracts;

public record LoginRequest(string Email, string Password, Guid? ClinicId = null);

public record LoginResponse(
    string? AccessToken,
    string? RefreshToken,
    DateTimeOffset? ExpiresAt,
    UserDto? User,
    bool RequiresTenantSelection = false,
    string? TenantSelectionToken = null,
    IReadOnlyList<ClinicMembershipDto>? AvailableClinics = null);

public record ClinicMembershipDto(Guid ClinicId, string ClinicName, string Role);
public record SelectTenantRequest(string TenantSelectionToken, Guid ClinicId);
public record RefreshRequest(string RefreshToken);
public record CreateUserRequest(string Email, string Password, string FullName, string Role, Guid? ClinicId = null);
public record UserDto(Guid Id, string Email, string FullName, string Role, Guid ClinicId, IReadOnlyList<string> Permissions);

public record BreakGlassRequest(Guid ClinicId, string TicketNumber, string Reason, int DurationMinutes);
public record BreakGlassResponse(string AccessToken, DateTimeOffset ExpiresAt, Guid SessionId, Guid ClinicId);

public interface IIdentityModule
{
    Task<LoginResponse?> LoginAsync(LoginRequest request, string? clientIp = null, CancellationToken cancellationToken = default);
    Task<LoginResponse?> SelectTenantAsync(SelectTenantRequest request, string? clientIp = null, CancellationToken cancellationToken = default);
    Task<LoginResponse?> RefreshAsync(RefreshRequest request, string? clientIp = null, CancellationToken cancellationToken = default);
    Task LogoutAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task<UserDto?> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<UserDto> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UserDto>> GetUsersAsync(CancellationToken cancellationToken = default);
    Task AssignRoleAsync(Guid userId, string role, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetUserPermissionsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<BreakGlassResponse> InitiateBreakGlassAsync(Guid superAdminUserId, BreakGlassRequest request, CancellationToken cancellationToken = default);
}
