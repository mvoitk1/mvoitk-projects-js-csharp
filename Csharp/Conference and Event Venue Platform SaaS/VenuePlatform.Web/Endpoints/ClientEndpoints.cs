using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using VenuePlatform.BLL.Domain.Clients;
using VenuePlatform.BLL.Tenancy;
using VenuePlatform.Contracts.Auth;
using VenuePlatform.DAL.Persistence;

namespace VenuePlatform.Web.Endpoints;

/// <summary>
/// Client management endpoints (tenant-scoped).
/// </summary>
public static class ClientEndpoints
{
    public static RouteGroupBuilder MapClientEndpoints(this RouteGroupBuilder group)
    {
        // POST /{companySlug}/clients/seed-one - Creates a test client for current tenant (requires auth + manager role)
        group.MapPost("/clients/seed-one", (ApplicationDbContext db, ITenantContext tenantContext, ClaimsPrincipal user) =>
        {
            // Extract userId from "sub" claim
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

            // Query membership with role
            var membership = db.UserCompanyMemberships
                .AsNoTracking()
                .FirstOrDefault(m => m.UserId == userId && m.CompanyId == tenant.CompanyId);

            if (membership is null)
            {
                return Results.Forbid();
            }

            // Allow only: CompanyOwner, CompanyAdmin, CompanyManager
            var allowedRoles = new[] { TenantRoles.CompanyOwner, TenantRoles.CompanyAdmin, TenantRoles.CompanyManager };
            if (!allowedRoles.Contains(membership.Role))
            {
                return Results.Forbid();
            }

            var client = new Client(tenant.CompanyId, "First Client", "Seeded");
            db.Clients.Add(client);
            db.SaveChanges();

            return Results.Created($"/{tenant.CompanySlug}/clients/{client.Id}", new { id = client.Id });
        })
        .RequireAuthorization();

        // GET /{companySlug}/clients - Lists clients for current tenant (requires auth + membership)
        group.MapGet("/clients", (ApplicationDbContext db, ITenantContext tenantContext, ClaimsPrincipal user) =>
        {
            // Extract userId from "sub" claim
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

            // Check membership
            var isMember = db.UserCompanyMemberships
                .AsNoTracking()
                .Any(m => m.UserId == userId && m.CompanyId == tenant.CompanyId);

            if (!isMember)
            {
                return Results.Forbid();
            }

            var clients = db.Clients
                .AsNoTracking()
                .OrderBy(c => c.Name)
                .Select(c => new { c.Id, c.Name, c.CompanyId })
                .ToList();

            return Results.Ok(clients);
        })
        .RequireAuthorization();

        // GET /{companySlug}/memberships - Lists user memberships for current tenant (query by CompanyId)
        group.MapGet("/memberships", (ApplicationDbContext db, ITenantContext tenantContext) =>
        {
            var tenant = tenantContext.Current;
            if (tenant is null)
            {
                return Results.NotFound();
            }

            var memberships = db.UserCompanyMemberships
                .AsNoTracking()
                .Where(m => m.CompanyId == tenant.CompanyId)
                .OrderBy(m => m.Role)
                .Select(m => new { m.UserId, m.CompanyId, m.Role, m.CreatedUtc })
                .ToList();

            return Results.Ok(memberships);
        });

        return group;
    }
}
