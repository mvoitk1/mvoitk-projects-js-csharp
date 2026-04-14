using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using VenuePlatform.BLL.Domain.Clients;
using VenuePlatform.BLL.Tenancy;
using VenuePlatform.Contracts.Auth;
using VenuePlatform.Contracts.Clients;
using VenuePlatform.Contracts.Common;
using VenuePlatform.DAL.Persistence;
using VenuePlatform.Web.Auth;

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

            return Results.Created($"/{tenant.CompanySlug}/clients/{client.Id}", new CreateEntityResponse(client.Id));
        })
        .RequireAuthorization();

        // GET /{companySlug}/clients - Lists clients for current tenant (requires auth + membership)
        group.MapGet("/clients", (ApplicationDbContext db, ITenantContext tenantContext, IUserContext userContext) =>
        {
            // Extract userId from IUserContext
            var userId = userContext.UserId;
            if (userId is null)
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
                .Any(m => m.UserId == userId.Value && m.CompanyId == tenant.CompanyId);

            if (!isMember)
            {
                return Results.Forbid();
            }

            var clients = db.Clients
                .AsNoTracking()
                .Where(c => c.CompanyId == tenant.CompanyId)
                .OrderBy(c => c.Name)
                .Select(c => new ClientResponse(c.Id, c.CompanyId, c.Name, c.Notes, c.Email, c.CreatedUtc))
                .ToList();

            return Results.Ok(clients);
        })
        .RequireAuthorization();

        // GET /{companySlug}/clients/{id} - Get a single client by ID (requires auth + membership)
        group.MapGet("/clients/{id:guid}", (ApplicationDbContext db, ITenantContext tenantContext, ClaimsPrincipal user, Guid id) =>
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

            var client = db.Clients
                .AsNoTracking()
                .Where(c => c.Id == id && c.CompanyId == tenant.CompanyId)
                .Select(c => new ClientResponse(c.Id, c.CompanyId, c.Name, c.Notes, c.Email, c.CreatedUtc))
                .FirstOrDefault();

            if (client is null)
            {
                return Results.NotFound();
            }

            return Results.Ok(client);
        })
        .RequireAuthorization();

        // POST /{companySlug}/clients - Create a new client (requires auth + Manager/Admin/Owner)
        group.MapPost("/clients", (ApplicationDbContext db, ITenantContext tenantContext, ClaimsPrincipal user, CreateClientRequest request) =>
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

            // Validate request
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return Results.BadRequest(new { error = "Name is required." });
            }

            var client = new Client(tenant.CompanyId, request.Name.Trim(), request.Notes);
            client.UpdateEmail(request.Email);
            db.Clients.Add(client);
            db.SaveChanges();

            return Results.Created($"/{tenant.CompanySlug}/clients/{client.Id}", new CreateEntityResponse(client.Id));
        })
        .RequireAuthorization();

        // PUT /{companySlug}/clients/{id} - Update an existing client (requires auth + Manager/Admin/Owner)
        group.MapPut("/clients/{id:guid}", (ApplicationDbContext db, ITenantContext tenantContext, ClaimsPrincipal user, Guid id, UpdateClientRequest request) =>
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

            // Validate request
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return Results.BadRequest(new { error = "Name is required." });
            }

            var client = db.Clients
                .FirstOrDefault(c => c.Id == id && c.CompanyId == tenant.CompanyId);

            if (client is null)
            {
                return Results.NotFound();
            }

            client.UpdateName(request.Name.Trim());
            client.SetNotes(request.Notes);
            client.UpdateEmail(request.Email);
            db.SaveChanges();

            var response = new ClientResponse(client.Id, client.CompanyId, client.Name, client.Notes, client.Email, client.CreatedUtc);
            return Results.Ok(response);
        })
        .RequireAuthorization();

        // DELETE /{companySlug}/clients/{id} - Delete a client (requires auth + Manager/Admin/Owner)
        group.MapDelete("/clients/{id:guid}", (ApplicationDbContext db, ITenantContext tenantContext, ClaimsPrincipal user, Guid id) =>
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

            var client = db.Clients
                .FirstOrDefault(c => c.Id == id && c.CompanyId == tenant.CompanyId);

            if (client is null)
            {
                return Results.NotFound();
            }

            db.Clients.Remove(client);
            db.SaveChanges();

            return Results.NoContent();
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
