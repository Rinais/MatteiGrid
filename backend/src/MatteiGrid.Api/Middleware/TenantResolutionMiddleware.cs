using System.Security.Claims;
using MatteiGrid.Shared.Infrastructure.Tenancy;
using MatteiGrid.Shared.Kernel;

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
                ?? context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? context.User.FindFirst("sub")?.Value;

            var roleClaim = context.User.FindFirst(ClaimTypes.Role)?.Value
                ?? context.User.FindFirst("role")?.Value
                ?? string.Empty;

            var isBreakGlassClaim = context.User.FindFirst("is_break_glass")?.Value;
            var isBreakGlass = string.Equals(isBreakGlassClaim, "true", StringComparison.OrdinalIgnoreCase);
            var breakGlassReason = context.User.FindFirst("break_glass_reason")?.Value;

            if (Guid.TryParse(clinicIdClaim, out var clinicId) && Guid.TryParse(userIdClaim, out var userId))
            {
                // AUTH-01: Detect and reject spoofed client-supplied tenant headers
                if (context.Request.Headers.TryGetValue("X-Clinic-Id", out var clientSuppliedHeader))
                {
                    if (Guid.TryParse(clientSuppliedHeader.FirstOrDefault(), out var headerClinicId) && headerClinicId != clinicId)
                    {
                        context.Response.StatusCode = StatusCodes.Status400BadRequest;
                        context.Response.ContentType = "application/json";
                        await context.Response.WriteAsJsonAsync(new
                        {
                            error = "TenantMismatch",
                            message = "Client-supplied X-Clinic-Id does not match the authenticated tenant claim."
                        });
                        return;
                    }
                }

                // Resolve permissions server-side from the role
                var permissions = Permissions.RolePermissions.TryGetValue(roleClaim, out var perms)
                    ? perms
                    : Array.Empty<string>();

                tenantContext.SetTenant(clinicId, userId, roleClaim, permissions, isBreakGlass, breakGlassReason);
            }
        }
        else
        {
            // AUTH-01: Drop unauthenticated X-Clinic-Id header fallback.
            // Client-supplied tenant headers are NEVER trusted for unauthenticated requests.
        }

        await _next(context);
    }
}
