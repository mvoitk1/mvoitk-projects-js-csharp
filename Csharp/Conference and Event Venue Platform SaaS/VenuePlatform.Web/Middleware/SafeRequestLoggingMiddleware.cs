using System.Diagnostics;
using System.Security.Claims;

namespace VenuePlatform.Web.Middleware;

/// <summary>
/// Minimal request logging middleware that avoids PII.
/// Logs: method, path, status code, elapsed ms, companySlug (if present), userId (if authenticated).
/// Does NOT log: Authorization headers, request body, query strings.
/// </summary>
public sealed class SafeRequestLoggingMiddleware(RequestDelegate next, ILogger<SafeRequestLoggingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        
        // Get route values early (before response)
        var method = context.Request.Method;
        var path = context.Request.Path;
        
        // Extract companySlug from route if present
        var companySlug = context.Request.RouteValues["companySlug"]?.ToString();
        
        // Extract userId from claims if authenticated
        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        try
        {
            await next(context);
        }
        finally
        {
            stopwatch.Stop();
            
            var statusCode = context.Response.StatusCode;
            var elapsedMs = stopwatch.ElapsedMilliseconds;
            
            // Build log scope with safe data only
            var logState = new Dictionary<string, object?>
            {
                ["Method"] = method,
                ["Path"] = path,
                ["StatusCode"] = statusCode,
                ["ElapsedMs"] = elapsedMs
            };
            
            if (!string.IsNullOrEmpty(companySlug))
            {
                logState["CompanySlug"] = companySlug;
            }
            
            if (!string.IsNullOrEmpty(userId))
            {
                logState["UserId"] = userId;
            }
            
            using (logger.BeginScope(logState))
            {
                logger.LogInformation("HTTP {Method} {Path} responded {StatusCode} in {ElapsedMs}ms", 
                    method, path, statusCode, elapsedMs);
            }
        }
    }
}
