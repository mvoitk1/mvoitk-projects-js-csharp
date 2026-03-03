using VenuePlatform.BLL.Domain.Companies;
using VenuePlatform.BLL.Tenancy;

namespace VenuePlatform.Web.Endpoints;

/// <summary>
/// Health check endpoints (platform + tenant scoped).
/// </summary>
public static class HealthEndpoints
{
    /// <summary>
    /// Maps platform-level health endpoint (no tenant required).
    /// </summary>
    public static IEndpointRouteBuilder MapPlatformHealthEndpoints(this IEndpointRouteBuilder app)
    {
        // GET /health - Platform health endpoint (exempt from tenant middleware check)
        app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTimeOffset.UtcNow }));

        return app;
    }

    /// <summary>
    /// Maps tenant-scoped health endpoints.
    /// </summary>
    public static RouteGroupBuilder MapTenantHealthEndpoints(this RouteGroupBuilder group)
    {
        // GET /{companySlug}/health - Health endpoint with tenant slug
        group.MapGet("/health", (ITenantContext tenantContext) =>
        {
            var tenant = tenantContext.Current;
            return tenant is null
                ? Results.Problem("Tenant not resolved", statusCode: 500)
                : Results.Ok(new { companyId = tenant.CompanyId, companySlug = tenant.CompanySlug });
        });

        // GET /{companySlug}/db-check - Verification endpoint for DbContext/Repository wiring (pure DI check - no DB access)
        group.MapGet("/db-check", (ICompanyRepository repo, ITenantContext tenantContext) =>
        {
            var tenant = tenantContext.Current;
            return tenant is null
                ? Results.NotFound()
                : Results.Ok(new { companyId = tenant.CompanyId, companySlug = tenant.CompanySlug, repoResolved = true, timestamp = DateTimeOffset.UtcNow });
        });

        return group;
    }
}
