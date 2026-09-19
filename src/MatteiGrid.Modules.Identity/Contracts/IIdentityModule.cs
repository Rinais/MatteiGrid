namespace MatteiGrid.Modules.Identity.Contracts;

public record LoginRequest(string Email, string Password);
public record LoginResponse(string AccessToken, string RefreshToken, DateTimeOffset ExpiresAt, UserDto User);
public record RefreshRequest(string RefreshToken);
public record CreateUserRequest(string Email, string Password, string FullName, string Role);
public record UserDto(Guid Id, string Email, string FullName, string Role, IReadOnlyList<string> Permissions);

public interface IIdentityModule
{
    Task<LoginResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<LoginResponse?> RefreshAsync(RefreshRequest request, CancellationToken cancellationToken = default);
    Task LogoutAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task<UserDto?> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<UserDto> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UserDto>> GetUsersAsync(CancellationToken cancellationToken = default);
    Task AssignRoleAsync(Guid userId, string role, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetUserPermissionsAsync(Guid userId, CancellationToken cancellationToken = default);
}
