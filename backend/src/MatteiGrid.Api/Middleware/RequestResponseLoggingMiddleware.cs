using System.Diagnostics;
using MatteiGrid.Shared.Infrastructure.Tenancy;
using Serilog.Context;

namespace MatteiGrid.Api.Middleware;

/// <summary>
/// Captures and logs all incoming HTTP requests and outgoing HTTP responses.
/// Tags log entries with LogType = 'HttpRequestResponse' so they route to matteigrid-requests-*.log.
/// Enriches Serilog LogContext with CorrelationId, ClinicId, and UserId.
/// </summary>
public class RequestResponseLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestResponseLoggingMiddleware> _logger;

    public RequestResponseLoggingMiddleware(RequestDelegate next, ILogger<RequestResponseLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ICurrentTenantContext tenantContext)
    {
        var traceId = context.TraceIdentifier;
        var clinicId = tenantContext.ClinicId?.ToString() ?? "Anonymous";
        var userId = tenantContext.UserId?.ToString() ?? "Anonymous";
        var method = context.Request.Method;
        var path = context.Request.Path.Value ?? "/";
        var query = context.Request.QueryString.HasValue ? context.Request.QueryString.Value : string.Empty;
        var clientIp = GetClientIp(context);

        // Enrich Serilog LogContext for all downstream log entries
        using (LogContext.PushProperty("CorrelationId", traceId))
        using (LogContext.PushProperty("ClinicId", clinicId))
        using (LogContext.PushProperty("UserId", userId))
        {
            // Log HTTP Request Entry with LogType property
            using (LogContext.PushProperty("LogType", "HttpRequestResponse"))
            {
                _logger.LogInformation(
                    "[HTTP REQUEST] {Method} {Path}{Query} from {ClientIp} | ClinicId: {ClinicId} | User: {UserId} | TraceId: {TraceId}",
                    method, path, query, clientIp, clinicId, userId, traceId);
            }

            var sw = Stopwatch.StartNew();

            try
            {
                await _next(context);
            }
            finally
            {
                sw.Stop();
                var statusCode = context.Response.StatusCode;
                var contentLength = context.Response.ContentLength?.ToString() ?? "chunked";

                // Log HTTP Response Exit with LogType property
                using (LogContext.PushProperty("LogType", "HttpRequestResponse"))
                {
                    _logger.LogInformation(
                        "[HTTP RESPONSE] {Method} {Path} -> {StatusCode} (Elapsed: {ElapsedMilliseconds} ms) | Size: {ContentLength} | TraceId: {TraceId}",
                        method, path, statusCode, sw.ElapsedMilliseconds, contentLength, traceId);
                }
            }
        }
    }

    private static string GetClientIp(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue("X-Forwarded-For", out var forwarded))
        {
            var ip = forwarded.FirstOrDefault()?.Split(',').FirstOrDefault()?.Trim();
            if (!string.IsNullOrWhiteSpace(ip)) return ip;
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
    }
}
