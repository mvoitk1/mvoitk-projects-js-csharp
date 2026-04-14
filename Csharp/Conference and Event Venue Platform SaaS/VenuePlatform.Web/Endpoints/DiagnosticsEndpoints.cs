using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VenuePlatform.BLL.Tenancy;
using VenuePlatform.Contracts.Diagnostics;
using VenuePlatform.DAL.Persistence;

namespace VenuePlatform.Web.Endpoints;

/// <summary>
/// Diagnostics endpoints for platform health and tenant diagnostics.
/// </summary>
public static class DiagnosticsEndpoints
{
    /// <summary>
    /// Maps platform-level health endpoint (no tenant required).
    /// </summary>
    public static IEndpointRouteBuilder MapPlatformDiagnosticsEndpoints(this IEndpointRouteBuilder app)
    {
        // GET /health - Platform health check (anonymous allowed)
        app.MapGet("/health", async (ApplicationDbContext db, IWebHostEnvironment env) =>
        {
            bool dbOk = false;
            try
            {
                dbOk = await db.Database.CanConnectAsync();
            }
            catch
            {
                // Do not expose exception details to client
                dbOk = false;
            }

            var response = new PlatformHealthResponse(
                UtcNow: DateTime.UtcNow,
                Environment: env.EnvironmentName,
                DatabaseOk: dbOk
            );

            return dbOk ? Results.Ok(response) : Results.Problem(
                statusCode: 503,
                title: "Service Unavailable",
                detail: "Database connectivity check failed"
            );
        })
        .AllowAnonymous()
        .WithName("PlatformHealth")
        .WithDescription("Platform-wide health check endpoint. Anonymous access.");

        return app;
    }

    /// <summary>
    /// Maps tenant-scoped diagnostics endpoints.
    /// </summary>
    public static RouteGroupBuilder MapTenantDiagnosticsEndpoints(this RouteGroupBuilder group)
    {
        // GET /{companySlug}/diagnostics - Tenant diagnostics (auth required)
        group.MapGet("/diagnostics", async (
            ApplicationDbContext db,
            ITenantContext tenantContext) =>
        {
            var tenant = tenantContext.Current;
            if (tenant is null)
            {
                return Results.Problem(
                    statusCode: 500,
                    title: "Tenant not resolved");
            }

            // Check database connectivity
            bool dbOk = false;
            try
            {
                dbOk = await db.Database.CanConnectAsync();
            }
            catch
            {
                // Do not expose exception details
                dbOk = false;
            }

            // Calculate month boundaries (UTC)
            var nowUtc = DateTime.UtcNow;
            var monthStart = new DateTime(nowUtc.Year, nowUtc.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var nextMonthStart = monthStart.AddMonths(1);

            // Count tenant spaces (include inactive)
            var spaceCount = await db.Spaces
                .AsNoTracking()
                .Where(s => s.CompanyId == tenant.CompanyId)
                .CountAsync();

            // Count bookings created this month
            var bookingCount = await db.Bookings
                .AsNoTracking()
                .Where(b => b.CompanyId == tenant.CompanyId)
                .Where(b => b.CreatedUtc >= monthStart && b.CreatedUtc < nextMonthStart)
                .CountAsync();

            // Count invoices created this month
            var invoiceCount = await db.Invoices
                .AsNoTracking()
                .Where(i => i.CompanyId == tenant.CompanyId)
                .Where(i => i.CreatedUtc >= monthStart && i.CreatedUtc < nextMonthStart)
                .CountAsync();

            var response = new TenantDiagnosticsResponse(
                UtcNow: nowUtc,
                CompanySlug: tenant.CompanySlug,
                CompanyId: tenant.CompanyId,
                DatabaseOk: dbOk,
                SpaceCount: spaceCount,
                BookingCountThisMonth: bookingCount,
                InvoiceCountThisMonth: invoiceCount
            );

            return Results.Ok(response);
        })
        .RequireAuthorization()
        .WithName("TenantDiagnostics")
        .WithDescription("Tenant diagnostics endpoint. Requires authentication and tenant membership.");

        return group;
    }
}
