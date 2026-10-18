using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FluentAssertions;
using MatteiGrid.Api.Authorization;
using MatteiGrid.Api.Middleware;
using MatteiGrid.Modules.Identity.Services;
using MatteiGrid.Shared.Infrastructure.Tenancy;
using MatteiGrid.Shared.Kernel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace MatteiGrid.UnitTests;

public class AuthUnitTests
{
    private readonly IConfiguration _config;
    private readonly JwtTokenService _jwtService;

    public AuthUnitTests()
    {
        var inMemorySettings = new Dictionary<string, string?>
        {
            ["Jwt:Key"] = "SuperSecretUnitTestingKeyForMatteiGrid1234567890!",
            ["Jwt:Issuer"] = "MatteiGridApi",
            ["Jwt:Audience"] = "MatteiGridClients"
        };

        _config = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        _jwtService = new JwtTokenService(_config);
    }

    [Fact]
    public void JwtTokenService_ShouldGenerate_AccessToken_With15MinTtl_AndLeanClaims()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var clinicId = Guid.NewGuid();
        var email = "doctor@matteigrid.com";
        var role = Roles.Doctor;

        // Act
        var (token, expiresAt) = _jwtService.GenerateAccessToken(userId, email, clinicId, role);

        // Assert
        expiresAt.Should().BeCloseTo(DateTimeOffset.UtcNow.AddMinutes(15), TimeSpan.FromSeconds(5));

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        jwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.Sub).Value.Should().Be(userId.ToString());
        jwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.Email).Value.Should().Be(email);
        jwt.Claims.First(c => c.Type == "clinic_id").Value.Should().Be(clinicId.ToString());
        jwt.Claims.First(c => c.Type == "role").Value.Should().Be(role);
        jwt.Claims.First(c => c.Type == "token_version").Value.Should().Be("1");

        // AUTH-04: Lean claims check - permissions should NOT be serialized into JWT
        jwt.Claims.Any(c => c.Type == "permissions").Should().BeFalse();
    }

    [Fact]
    public void JwtTokenService_TenantSelectionToken_ShouldValidate_Correctly()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var email = "visiting.doctor@matteigrid.com";

        // Act
        var token = _jwtService.GenerateTenantSelectionToken(userId, email);
        var validated = _jwtService.ValidateTenantSelectionToken(token);

        // Assert
        validated.Should().NotBeNull();
        validated!.Value.UserId.Should().Be(userId);
        validated!.Value.Email.Should().Be(email);
    }

    [Fact]
    public async Task TenantResolutionMiddleware_ShouldReject_Mismatched_XClinicId()
    {
        // Arrange
        var tenantContext = new CurrentTenantContext();
        var tokenClinicId = Guid.NewGuid();
        var spoofedClinicId = Guid.NewGuid();

        var middleware = new TenantResolutionMiddleware(_ => Task.CompletedTask);
        var httpContext = new DefaultHttpContext();

        var claims = new[]
        {
            new Claim("clinic_id", tokenClinicId.ToString()),
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, Roles.Doctor)
        };
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Bearer"));
        httpContext.Request.Headers["X-Clinic-Id"] = spoofedClinicId.ToString();

        // Act
        await middleware.InvokeAsync(httpContext, tenantContext);

        // Assert (AUTH-01: Spoofed header results in 400 Bad Request)
        httpContext.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        tenantContext.ClinicId.Should().BeNull();
    }

    [Fact]
    public async Task TenantResolutionMiddleware_ShouldIgnore_Unauthenticated_XClinicId()
    {
        // Arrange
        var tenantContext = new CurrentTenantContext();
        var middleware = new TenantResolutionMiddleware(_ => Task.CompletedTask);
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["X-Clinic-Id"] = Guid.NewGuid().ToString();

        // Act
        await middleware.InvokeAsync(httpContext, tenantContext);

        // Assert (AUTH-01: Unauthenticated request cannot set tenant context via header)
        tenantContext.ClinicId.Should().BeNull();
        tenantContext.IsAuthenticated.Should().BeFalse();
    }

    [Fact]
    public async Task TenantResolutionMiddleware_ShouldDeriveTenant_FromJwtClinicIdClaim()
    {
        // Arrange
        var tenantContext = new CurrentTenantContext();
        var tokenClinicId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var middleware = new TenantResolutionMiddleware(_ => Task.CompletedTask);
        var httpContext = new DefaultHttpContext();

        var claims = new[]
        {
            new Claim("clinic_id", tokenClinicId.ToString()),
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, Roles.Doctor)
        };
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Bearer"));

        // Act
        await middleware.InvokeAsync(httpContext, tenantContext);

        // Assert
        tenantContext.ClinicId.Should().Be(tokenClinicId);
        tenantContext.UserId.Should().Be(userId);
        tenantContext.Role.Should().Be(Roles.Doctor);
        tenantContext.HasPermission(Permissions.ConsultationsRead).Should().BeTrue();
    }

    [Fact]
    public async Task PermissionAuthorizationHandler_ShouldDeny_SuperAdmin_ClinicalAccess_WithoutBreakGlass()
    {
        // Arrange (AUTH-05: SuperAdmin has NO default access to clinical PHI)
        var tenantContext = new CurrentTenantContext();
        tenantContext.SetTenant(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Roles.SuperAdmin,
            Permissions.RolePermissions[Roles.SuperAdmin],
            isBreakGlass: false);

        var handler = new PermissionAuthorizationHandler(tenantContext);

        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Role, Roles.SuperAdmin)
        }, "Bearer"));

        var requirement = new HasPermissionRequirement(Permissions.ConsultationsRead);
        var authContext = new AuthorizationHandlerContext(new[] { requirement }, user, null);

        // Act
        await handler.HandleAsync(authContext);

        // Assert
        authContext.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task PermissionAuthorizationHandler_ShouldAllow_SuperAdmin_ClinicalAccess_WithActiveBreakGlass()
    {
        // Arrange (AUTH-05: SuperAdmin WITH active break-glass session is granted access)
        var tenantContext = new CurrentTenantContext();
        tenantContext.SetTenant(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Roles.SuperAdmin,
            Permissions.RolePermissions[Roles.SuperAdmin],
            isBreakGlass: true,
            breakGlassReason: "Emergency incident INC-98762: Patient records locked in clinical consultation.");

        var handler = new PermissionAuthorizationHandler(tenantContext);

        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Role, Roles.SuperAdmin)
        }, "Bearer"));

        var requirement = new HasPermissionRequirement(Permissions.ConsultationsRead);
        var authContext = new AuthorizationHandlerContext(new[] { requirement }, user, null);

        // Act
        await handler.HandleAsync(authContext);

        // Assert
        authContext.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public void Permissions_RolePermissions_LabTechnician_ShouldNotHave_PatientsRead()
    {
        // Assert per matrix: Lab Tech has NO Patients.Read permission
        var labTechPermissions = Permissions.RolePermissions[Roles.LabTechnician];
        labTechPermissions.Should().NotContain(Permissions.PatientsRead);
        labTechPermissions.Should().Contain(Permissions.LaboratoryRead);
    }

    [Fact]
    public void Permissions_RolePermissions_Doctor_ShouldHave_InventoryRead()
    {
        // Assert per matrix: Doctor has Inventory.Read permission
        var doctorPermissions = Permissions.RolePermissions[Roles.Doctor];
        doctorPermissions.Should().Contain(Permissions.InventoryRead);
    }

    [Fact]
    public async Task PermissionAuthorizationHandler_ShouldAllow_SuperAdmin_AdministrationManageUsers_WithoutBreakGlass()
    {
        // Arrange: SuperAdmin has platform management permissions (including Administration.ManageUsers) by default
        var tenantContext = new CurrentTenantContext();
        tenantContext.SetTenant(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Roles.SuperAdmin,
            Permissions.RolePermissions[Roles.SuperAdmin],
            isBreakGlass: false);

        var handler = new PermissionAuthorizationHandler(tenantContext);
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Role, Roles.SuperAdmin)
        }, "Bearer"));

        var requirement = new HasPermissionRequirement(Permissions.AdministrationManageUsers);
        var authContext = new AuthorizationHandlerContext(new[] { requirement }, user, null);

        // Act
        await handler.HandleAsync(authContext);

        // Assert
        authContext.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task PermissionAuthorizationHandler_ShouldDeny_Doctor_AdministrationManageUsers()
    {
        // Arrange: Doctor does NOT have user administration or password reset permissions
        var tenantContext = new CurrentTenantContext();
        tenantContext.SetTenant(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Roles.Doctor,
            Permissions.RolePermissions[Roles.Doctor],
            isBreakGlass: false);

        var handler = new PermissionAuthorizationHandler(tenantContext);
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Role, Roles.Doctor)
        }, "Bearer"));

        var requirement = new HasPermissionRequirement(Permissions.AdministrationManageUsers);
        var authContext = new AuthorizationHandlerContext(new[] { requirement }, user, null);

        // Act
        await handler.HandleAsync(authContext);

        // Assert
        authContext.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task PermissionAuthorizationHandler_ShouldAllow_Administrator_AdministrationManageUsers()
    {
        // Arrange: Clinic Administrator has Administration.ManageUsers permission
        var tenantContext = new CurrentTenantContext();
        tenantContext.SetTenant(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Roles.Administrator,
            Permissions.RolePermissions[Roles.Administrator],
            isBreakGlass: false);

        var handler = new PermissionAuthorizationHandler(tenantContext);
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Role, Roles.Administrator)
        }, "Bearer"));

        var requirement = new HasPermissionRequirement(Permissions.AdministrationManageUsers);
        var authContext = new AuthorizationHandlerContext(new[] { requirement }, user, null);

        // Act
        await handler.HandleAsync(authContext);

        // Assert
        authContext.HasSucceeded.Should().BeTrue();
    }
}
