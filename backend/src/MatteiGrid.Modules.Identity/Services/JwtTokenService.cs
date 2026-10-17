using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace MatteiGrid.Modules.Identity.Services;

public class JwtTokenService
{
    private readonly IConfiguration _configuration;

    public JwtTokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <summary>
    /// Generates short-lived access token (<=15 minutes per AUTH-04) with lean identity claims.
    /// Permissions are resolved server-side and NOT bloated into the JWT payload.
    /// </summary>
    public (string Token, DateTimeOffset ExpiresAt) GenerateAccessToken(
        Guid userId,
        string email,
        Guid clinicId,
        string role,
        int tokenVersion = 1,
        bool isBreakGlass = false,
        string? breakGlassReason = null)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(GetJwtKey()));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTimeOffset.UtcNow.AddMinutes(15);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Email, email),
            new("clinic_id", clinicId.ToString()),
            new("role", role),
            new("token_version", tokenVersion.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        if (isBreakGlass)
        {
            claims.Add(new Claim("is_break_glass", "true"));
            if (!string.IsNullOrWhiteSpace(breakGlassReason))
            {
                claims.Add(new Claim("break_glass_reason", breakGlassReason));
            }
        }

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"] ?? "MatteiGridApi",
            audience: _configuration["Jwt:Audience"] ?? "MatteiGridClients",
            claims: claims,
            expires: expires.UtcDateTime,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expires);
    }

    /// <summary>
    /// Generates a short-lived (5-minute) tenant selection token for multi-clinic users.
    /// </summary>
    public string GenerateTenantSelectionToken(Guid userId, string email)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(GetJwtKey()));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTimeOffset.UtcNow.AddMinutes(5);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Email, email),
            new("purpose", "tenant_selection"),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"] ?? "MatteiGridApi",
            audience: _configuration["Jwt:Audience"] ?? "MatteiGridClients",
            claims: claims,
            expires: expires.UtcDateTime,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Validates tenant selection token and extracts userId and email.
    /// </summary>
    public (Guid UserId, string Email)? ValidateTenantSelectionToken(string token)
    {
        var handler = new JwtSecurityTokenHandler();
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(GetJwtKey()));

        try
        {
            var principal = handler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ClockSkew = TimeSpan.Zero
            }, out _);

            var purpose = principal.FindFirst("purpose")?.Value;
            if (purpose != "tenant_selection") return null;

            var sub = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                      ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var email = principal.FindFirst(JwtRegisteredClaimNames.Email)?.Value
                        ?? principal.FindFirst(ClaimTypes.Email)?.Value;

            if (Guid.TryParse(sub, out var userId) && !string.IsNullOrWhiteSpace(email))
            {
                return (userId, email);
            }
        }
        catch
        {
            return null;
        }

        return null;
    }

    public string GenerateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes);
    }

    public string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(bytes);
    }

    private string GetJwtKey()
    {
        return _configuration["Jwt:Key"] ?? "MatteiGridDefaultSecretKey123456789012345!";
    }
}
