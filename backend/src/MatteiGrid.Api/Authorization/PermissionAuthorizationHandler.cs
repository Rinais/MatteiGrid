using MatteiGrid.Shared.Infrastructure.Tenancy;
using MatteiGrid.Shared.Kernel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace MatteiGrid.Api.Authorization;

public class HasPermissionRequirement : IAuthorizationRequirement
{
    public string Permission { get; }

    public HasPermissionRequirement(string permission)
    {
        Permission = permission;
    }
}

public class PermissionAuthorizationHandler : AuthorizationHandler<HasPermissionRequirement>
{
    private readonly ICurrentTenantContext _tenantContext;

    public PermissionAuthorizationHandler(ICurrentTenantContext tenantContext)
    {
        _tenantContext = tenantContext;
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        HasPermissionRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            return Task.CompletedTask;
        }

        // Standard tenant permission check
        if (_tenantContext.HasPermission(requirement.Permission))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        // AUTH-05: SuperAdmin access model (DPDP compliance)
        // SuperAdmin role does NOT automatically bypass clinical permissions.
        // Clinical data (Consultations, Prescriptions, Patients) is only accessible via active break-glass session!
        if (string.Equals(_tenantContext.Role, Roles.SuperAdmin, StringComparison.OrdinalIgnoreCase))
        {
            if (_tenantContext.IsBreakGlassSession)
            {
                // Active, audited break-glass session authorized
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            // Platform ops permissions granted to SuperAdmin
            if (requirement.Permission.StartsWith("SuperAdmin.", StringComparison.OrdinalIgnoreCase))
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            // Blanket SuperAdmin access to PHI is strictly blocked
            return Task.CompletedTask;
        }

        return Task.CompletedTask;
    }
}

public class PermissionPolicyProvider : DefaultAuthorizationPolicyProvider
{
    private const string PolicyPrefix = "Permission:";

    public PermissionPolicyProvider(IOptions<AuthorizationOptions> options) : base(options)
    {
    }

    public override async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (policyName.StartsWith(PolicyPrefix, StringComparison.OrdinalIgnoreCase))
        {
            var permission = policyName[PolicyPrefix.Length..];
            return new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(new HasPermissionRequirement(permission))
                .Build();
        }

        return await base.GetPolicyAsync(policyName);
    }
}

public static class EndpointAuthorizationExtensions
{
    public static TBuilder RequirePermission<TBuilder>(this TBuilder builder, string permission)
        where TBuilder : IEndpointConventionBuilder
    {
        return builder.RequireAuthorization($"Permission:{permission}");
    }
}
