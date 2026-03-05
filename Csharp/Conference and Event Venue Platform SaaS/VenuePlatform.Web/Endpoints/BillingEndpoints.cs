using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using VenuePlatform.BLL.Limits;
using VenuePlatform.BLL.Tenancy;
using VenuePlatform.Contracts.Billing;
using VenuePlatform.DAL.Persistence;

namespace VenuePlatform.Web.Endpoints;

/// <summary>
/// Billing and plan management endpoints (tenant-scoped).
/// </summary>
public static class BillingEndpoints
{
    public static RouteGroupBuilder MapBillingEndpoints(this RouteGroupBuilder group)
    {
        // GET /{companySlug}/billing/plan - Get plan usage and limits (requires auth + any membership)
        group.MapGet("/billing/plan", async (ApplicationDbContext db, ITenantContext tenantContext, ClaimsPrincipal user) =>
        {
            // Extract userId from claims
            var userId = EndpointHelpers.GetUserIdFromClaims(user);
            if (userId == Guid.Empty)
            {
                return Results.Unauthorized();
            }

            var tenant = tenantContext.Current;
            if (tenant is null)
            {
                return Results.NotFound();
            }

            // Check membership - any role allowed
            var isMember = db.UserCompanyMemberships
                .AsNoTracking()
                .Any(m => m.UserId == userId && m.CompanyId == tenant.CompanyId);

            if (!isMember)
            {
                return Results.Forbid();
            }

            // 1) Load Company using tenant company id (not slug lookup)
            var company = await db.Companies
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == tenant.CompanyId);

            if (company is null)
            {
                return Results.NotFound();
            }

            // 2) Determine plan
            var plan = company.Plan;

            // 3) Compute limits using PlanLimits
            var maxSpaces = PlanLimits.MaxSpaces(plan);
            var maxBookingsPerMonth = PlanLimits.MaxBookingsPerMonth(plan);

            // 4) Count current tenant Spaces (all, including inactive)
            var currentSpaces = await db.Spaces
                .AsNoTracking()
                .CountAsync(s => s.CompanyId == tenant.CompanyId);

            // 5) Compute monthStartUtc/monthEndUtc based on DateTime.UtcNow (calendar month)
            var nowUtc = DateTime.UtcNow;
            var monthStartUtc = new DateTime(nowUtc.Year, nowUtc.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var monthEndUtc = monthStartUtc.AddMonths(1);

            // 6) Count bookings created this month
            var currentBookingsThisMonth = await db.Bookings
                .AsNoTracking()
                .CountAsync(b => b.CompanyId == tenant.CompanyId
                    && b.CreatedUtc >= monthStartUtc
                    && b.CreatedUtc < monthEndUtc);

            // 7) Return PlanUsageResponse
            var response = new PlanUsageResponse(
                CompanyName: company.Name,
                CompanySlug: company.Slug,
                Plan: plan,
                MaxSpaces: maxSpaces,
                CurrentSpaces: currentSpaces,
                MaxBookingsPerMonth: maxBookingsPerMonth,
                CurrentBookingsThisMonth: currentBookingsThisMonth,
                MonthStartUtc: monthStartUtc,
                MonthEndUtc: monthEndUtc
            );

            return Results.Ok(response);
        })
        .RequireAuthorization();

        return group;
    }
}
