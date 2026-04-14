using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using VenuePlatform.BLL.Domain.Spaces;
using VenuePlatform.BLL.Tenancy;
using VenuePlatform.Contracts.Auth;
using VenuePlatform.Contracts.Common;
using VenuePlatform.Contracts.Spaces;
using VenuePlatform.DAL.Persistence;
using VenuePlatform.Web.Auth;

namespace VenuePlatform.Web.Endpoints;

/// <summary>
/// Space management endpoints (tenant-scoped).
/// </summary>
public static class SpaceEndpoints
{
    public static RouteGroupBuilder MapSpaceEndpoints(this RouteGroupBuilder group)
    {
        // GET /{companySlug}/spaces - Lists spaces for current tenant (requires auth + membership)
        group.MapGet("/spaces", (ApplicationDbContext db, ITenantContext tenantContext, IUserContext userContext) =>
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

            var spaces = db.Spaces
                .AsNoTracking()
                .Where(s => s.CompanyId == tenant.CompanyId)
                .OrderBy(s => s.Name)
                .Select(s => new SpaceResponse(s.Id, s.CompanyId, s.Name, s.Capacity, s.HourlyRate, s.Notes, s.IsActive))
                .ToList();

            return Results.Ok(spaces);
        })
        .RequireAuthorization();

        // POST /{companySlug}/spaces - Creates a new space for current tenant (requires auth + Manager/Admin/Owner)
        group.MapPost("/spaces", (ApplicationDbContext db, ITenantContext tenantContext, ClaimsPrincipal user, CreateSpaceRequest request) =>
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
            if (request.Capacity <= 0)
            {
                return Results.BadRequest(new { error = "Capacity must be greater than 0." });
            }
            if (request.HourlyRate < 0)
            {
                return Results.BadRequest(new { error = "Hourly rate cannot be negative." });
            }

            var space = new Space(tenant.CompanyId, request.Name.Trim(), request.Capacity, request.HourlyRate, request.Notes);
            db.Spaces.Add(space);
            db.SaveChanges();

            return Results.Created($"/{tenant.CompanySlug}/spaces/{space.Id}", new CreateEntityResponse(space.Id));
        })
        .RequireAuthorization();

        // GET /{companySlug}/spaces/{id} - Get a specific space by ID (requires auth + membership)
        group.MapGet("/spaces/{id:guid}", (ApplicationDbContext db, ITenantContext tenantContext, ClaimsPrincipal user, Guid id) =>
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

            var space = db.Spaces
                .AsNoTracking()
                .Where(s => s.Id == id && s.CompanyId == tenant.CompanyId)
                .Select(s => new SpaceResponse(s.Id, s.CompanyId, s.Name, s.Capacity, s.HourlyRate, s.Notes, s.IsActive))
                .FirstOrDefault();

            if (space is null)
            {
                return Results.NotFound();
            }

            return Results.Ok(space);
        })
        .RequireAuthorization();

        // PUT /{companySlug}/spaces/{id} - Update an existing space (requires auth + Manager/Admin/Owner)
        group.MapPut("/spaces/{id:guid}", (ApplicationDbContext db, ITenantContext tenantContext, ClaimsPrincipal user, Guid id, UpdateSpaceRequest request) =>
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
            if (request.Capacity <= 0)
            {
                return Results.BadRequest(new { error = "Capacity must be greater than 0." });
            }
            if (request.HourlyRate < 0)
            {
                return Results.BadRequest(new { error = "Hourly rate cannot be negative." });
            }

            var space = db.Spaces
                .FirstOrDefault(s => s.Id == id && s.CompanyId == tenant.CompanyId);

            if (space is null)
            {
                return Results.NotFound();
            }

            space.UpdateName(request.Name.Trim());
            space.UpdateCapacity(request.Capacity);
            space.UpdateHourlyRate(request.HourlyRate);
            space.SetNotes(request.Notes);
            db.SaveChanges();

            var response = new SpaceResponse(space.Id, space.CompanyId, space.Name, space.Capacity, space.HourlyRate, space.Notes, space.IsActive);
            return Results.Ok(response);
        })
        .RequireAuthorization();

        // POST /{companySlug}/spaces/{id}/deactivate - Deactivate a space (requires auth + Manager/Admin/Owner)
        group.MapPost("/spaces/{id:guid}/deactivate", (ApplicationDbContext db, ITenantContext tenantContext, ClaimsPrincipal user, Guid id) =>
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

            var space = db.Spaces
                .FirstOrDefault(s => s.Id == id && s.CompanyId == tenant.CompanyId);

            if (space is null)
            {
                return Results.NotFound();
            }

            if (!space.IsActive)
            {
                var existingResponse = new SpaceResponse(space.Id, space.CompanyId, space.Name, space.Capacity, space.HourlyRate, space.Notes, space.IsActive);
                return Results.Ok(existingResponse);
            }

            space.Deactivate();
            db.SaveChanges();

            var response = new SpaceResponse(space.Id, space.CompanyId, space.Name, space.Capacity, space.HourlyRate, space.Notes, space.IsActive);
            return Results.Ok(response);
        })
        .RequireAuthorization();

        // GET /{companySlug}/spaces/availability - Search available spaces for time range (requires auth + membership)
        group.MapGet("/spaces/availability", (ApplicationDbContext db, ITenantContext tenantContext, ClaimsPrincipal user, DateTime startUtc, DateTime endUtc, int? minCapacity, bool? onlyActive) =>
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

            // Validate time range
            if (startUtc >= endUtc)
            {
                return Results.BadRequest(new { error = "Start time must be before end time." });
            }

            // KISS safeguard: prevent absurdly large ranges (> 365 days)
            var maxRangeDays = 365;
            if ((endUtc - startUtc).TotalDays > maxRangeDays)
            {
                return Results.BadRequest(new { error = $"Time range cannot exceed {maxRangeDays} days." });
            }

            // Find "busy" space ids from overlapping non-cancelled bookings
            // Overlap rule: requestStartUtc < existingEndUtc && requestEndUtc > existingStartUtc
            var busySpaceIds = db.Bookings
                .AsNoTracking()
                .Where(b => !b.IsCancelled)
                .Where(b => b.StartUtc < endUtc && b.EndUtc > startUtc)
                .Join(db.BookingSpaces.AsNoTracking(), b => b.Id, bs => bs.BookingId, (b, bs) => bs.SpaceId)
                .Distinct()
                .ToList();

            // Build base query for spaces
            var query = db.Spaces.AsNoTracking();

            // Apply onlyActive filter (default true)
            var activeFilter = onlyActive ?? true;
            if (activeFilter)
            {
                query = query.Where(s => s.IsActive);
            }

            // Apply minCapacity filter if provided
            if (minCapacity.HasValue && minCapacity.Value > 0)
            {
                query = query.Where(s => s.Capacity >= minCapacity.Value);
            }

            // Return spaces NOT in busy list
            var availableSpaces = query
                .Where(s => !busySpaceIds.Contains(s.Id))
                .OrderBy(s => s.Name)
                .Select(s => new AvailableSpaceResponse(s.Id, s.Name, s.Capacity))
                .ToList();

            return Results.Ok(availableSpaces);
        })
        .RequireAuthorization();

        return group;
    }
}
