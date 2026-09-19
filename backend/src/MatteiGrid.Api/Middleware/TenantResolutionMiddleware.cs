using System.Security.Claims;
using MatteiGrid.Shared.Infrastructure.Tenancy;

namespace MatteiGrid.Api.Middleware;

public class TenantResolutionMiddleware
{
    private readonly RequestDelegate _next;

    public TenantResolutionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ICurrentTenantContext tenantContext)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var clinicIdClaim = context.User.FindFirst("clinic_id")?.Value;
            var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? context.User.FindFirst("sub")?.Value;

            var permissionsClaim = context.User.FindFirst("permissions")?.Value;
            var permissions = !string.IsNullOrWhiteSpace(permissionsClaim)
                ? permissionsClaim.Split(',', StringSplitOptions.RemoveEmptyEntries)
                : Array.Empty<string>();

            if (Guid.TryParse(clinicIdClaim, out var clinicId) && Guid.TryParse(userIdClaim, out var userId))
            {
                tenantContext.SetTenant(clinicId, userId, permissions);
            }
        }
        else if (context.Request.Headers.TryGetValue("X-Clinic-Id", out var clinicHeader) &&
                 Guid.TryParse(clinicHeader.FirstOrDefault(), out var headerClinicId))
        {
            // Optional dev / local unauthenticated clinic context support
            tenantContext.SetTenant(headerClinicId, Guid.Empty, Array.Empty<string>());
        }

        await _next(context);
    }
}
