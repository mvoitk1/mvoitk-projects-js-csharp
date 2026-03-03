using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using VenuePlatform.BLL.Domain.Spaces;
using VenuePlatform.BLL.Tenancy;
using VenuePlatform.Contracts.Auth;
using VenuePlatform.Contracts.Spaces;
using VenuePlatform.DAL.Persistence;

namespace VenuePlatform.Web.Endpoints;

/// <summary>
/// Space configuration endpoints (tenant-scoped) for combinable rooms.
/// </summary>
public static class SpaceConfigurationEndpoints
{
    public static RouteGroupBuilder MapSpaceConfigurationEndpoints(this RouteGroupBuilder group)
    {
        // GET /{companySlug}/space-configurations - Lists space configurations for current tenant (requires auth + membership)
        group.MapGet("/space-configurations", (ApplicationDbContext db, ITenantContext tenantContext, ClaimsPrincipal user) =>
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

            var configs = db.SpaceConfigurations
                .AsNoTracking()
                .OrderBy(sc => sc.Name)
                .Select(sc => new { sc.Id, sc.Name, sc.HourlyRateOverride, sc.MinBookingMinutesOverride, sc.IsActive, sc.CompanyId })
                .ToList();

            return Results.Ok(configs);
        })
        .RequireAuthorization();

        // POST /{companySlug}/space-configurations - Creates a new space configuration for current tenant (requires auth + Manager/Admin/Owner)
        group.MapPost("/space-configurations", (ApplicationDbContext db, ITenantContext tenantContext, ClaimsPrincipal user, CreateSpaceConfigurationRequest request) =>
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
            if (request.HourlyRateOverride.HasValue && request.HourlyRateOverride.Value < 0)
            {
                return Results.BadRequest(new { error = "Hourly rate override cannot be negative." });
            }
            if (request.MinBookingMinutesOverride.HasValue && request.MinBookingMinutesOverride.Value <= 0)
            {
                return Results.BadRequest(new { error = "Minimum booking minutes override must be greater than 0." });
            }

            var config = new SpaceConfiguration(tenant.CompanyId, request.Name.Trim(), request.HourlyRateOverride, request.MinBookingMinutesOverride);
            db.SpaceConfigurations.Add(config);
            db.SaveChanges();

            return Results.Created($"/{tenant.CompanySlug}/space-configurations/{config.Id}", new { config.Id, config.Name, config.HourlyRateOverride, config.MinBookingMinutesOverride });
        })
        .RequireAuthorization();

        // GET /{companySlug}/space-configurations/{id} - Get a specific space configuration with included space IDs (requires auth + membership)
        group.MapGet("/space-configurations/{id:guid}", (ApplicationDbContext db, ITenantContext tenantContext, ClaimsPrincipal user, Guid id) =>
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

            var config = db.SpaceConfigurations
                .AsNoTracking()
                .Select(sc => new { sc.Id, sc.Name, sc.HourlyRateOverride, sc.MinBookingMinutesOverride, sc.IsActive, sc.CompanyId })
                .FirstOrDefault(sc => sc.Id == id && sc.CompanyId == tenant.CompanyId);

            if (config is null)
            {
                return Results.NotFound();
            }

            // Get associated space IDs
            var spaceIds = db.SpaceConfigurationSpaces
                .AsNoTracking()
                .Where(scs => scs.SpaceConfigurationId == id)
                .Select(scs => scs.SpaceId)
                .ToList();

            // Get space names for convenience
            var spaces = db.Spaces
                .AsNoTracking()
                .Where(s => spaceIds.Contains(s.Id))
                .Select(s => new { s.Id, s.Name })
                .ToList();

            return Results.Ok(new
            {
                config.Id,
                config.Name,
                config.HourlyRateOverride,
                config.MinBookingMinutesOverride,
                config.IsActive,
                config.CompanyId,
                SpaceIds = spaceIds,
                Spaces = spaces
            });
        })
        .RequireAuthorization();

        // PUT /{companySlug}/space-configurations/{id}/spaces - Replace included spaces for a configuration (requires auth + Manager/Admin/Owner)
        group.MapPut("/space-configurations/{id:guid}/spaces", async (ApplicationDbContext db, ITenantContext tenantContext, ClaimsPrincipal user, Guid id, SetSpaceConfigurationSpacesRequest request) =>
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

            // Verify the space configuration exists and belongs to this tenant
            var config = db.SpaceConfigurations
                .FirstOrDefault(sc => sc.Id == id && sc.CompanyId == tenant.CompanyId);

            if (config is null)
            {
                return Results.NotFound();
            }

            // Validate that all space IDs exist and belong to this tenant
            if (request.SpaceIds.Count > 0)
            {
                var requestedSpaceIds = request.SpaceIds.ToHashSet();
                var existingSpaces = db.Spaces
                    .AsNoTracking()
                    .Where(s => requestedSpaceIds.Contains(s.Id) && s.CompanyId == tenant.CompanyId)
                    .Select(s => s.Id)
                    .ToList();

                if (existingSpaces.Count != requestedSpaceIds.Count)
                {
                    var missingIds = requestedSpaceIds.Except(existingSpaces);
                    return Results.BadRequest(new { error = "One or more space IDs are invalid or do not belong to this tenant.", missingIds });
                }
            }

            // Remove existing associations
            var existingAssociations = db.SpaceConfigurationSpaces
                .Where(scs => scs.SpaceConfigurationId == id)
                .ToList();

            db.SpaceConfigurationSpaces.RemoveRange(existingAssociations);

            // Add new associations
            foreach (var spaceId in request.SpaceIds)
            {
                db.SpaceConfigurationSpaces.Add(new SpaceConfigurationSpace
                {
                    SpaceConfigurationId = id,
                    SpaceId = spaceId
                });
            }

            await db.SaveChangesAsync();

            // Return the updated space IDs
            var updatedSpaceIds = db.SpaceConfigurationSpaces
                .AsNoTracking()
                .Where(scs => scs.SpaceConfigurationId == id)
                .Select(scs => scs.SpaceId)
                .ToList();

            return Results.Ok(new
            {
                config.Id,
                config.Name,
                SpaceIds = updatedSpaceIds
            });
        })
        .RequireAuthorization();

        return group;
    }
}
