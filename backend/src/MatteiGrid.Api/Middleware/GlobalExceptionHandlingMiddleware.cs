using System.Text.Json;
using MatteiGrid.Shared.Infrastructure.Tenancy;

namespace MatteiGrid.Api.Middleware;

/// <summary>
/// Catches unhandled exceptions that escape the endpoint pipeline,
/// records a FATAL level error log with full diagnostic context,
/// and returns a secure, standardized RFC 7807 ProblemDetails payload.
/// </summary>
public class GlobalExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;

    public GlobalExceptionHandlingMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ICurrentTenantContext tenantContext)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            var traceId = context.TraceIdentifier;
            var clinicId = tenantContext.ClinicId?.ToString() ?? "Anonymous";
            var userId = tenantContext.UserId?.ToString() ?? "Anonymous";
            var method = context.Request.Method;
            var path = context.Request.Path.Value ?? "/";

            _logger.LogCritical(ex,
                "[FATAL] [SYSTEM_CRASH] Unhandled exception occurred on {Method} {Path}: {Message} | TraceId: {TraceId} | ClinicId: {ClinicId} | User: {UserId}",
                method, path, ex.Message, traceId, clinicId, userId);

            if (!context.Response.HasStarted)
            {
                context.Response.Clear();
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                context.Response.ContentType = "application/problem+json";

                var problem = new
                {
                    type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                    title = "An unexpected error occurred processing your request.",
                    status = StatusCodes.Status500InternalServerError,
                    detail = "An internal server error occurred. Please provide the TraceId to clinic support.",
                    traceId,
                    timestamp = DateTimeOffset.UtcNow
                };

                await context.Response.WriteAsync(JsonSerializer.Serialize(problem));
            }
        }
    }
}
