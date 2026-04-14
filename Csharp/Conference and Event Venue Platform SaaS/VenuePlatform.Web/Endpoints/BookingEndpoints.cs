using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using VenuePlatform.BLL.Domain.Bookings;
using VenuePlatform.BLL.Tenancy;
using VenuePlatform.Contracts.Auth;
using VenuePlatform.Contracts.Bookings;
using VenuePlatform.DAL.Persistence;

namespace VenuePlatform.Web.Endpoints;

/// <summary>
/// Booking management endpoints (tenant-scoped).
/// </summary>
public static class BookingEndpoints
{
    public static RouteGroupBuilder MapBookingEndpoints(this RouteGroupBuilder group)
    {
        // GET /{companySlug}/bookings - Lists bookings for current tenant (requires auth + any membership)
        group.MapGet("/bookings", (ApplicationDbContext db, ITenantContext tenantContext, ClaimsPrincipal user, DateTime? startUtc, DateTime? endUtc, Guid? clientId, bool? includeCancelled) =>
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

            // Validate date range
            if (startUtc.HasValue && endUtc.HasValue && startUtc.Value >= endUtc.Value)
            {
                return Results.BadRequest(new { error = "Start time must be before end time." });
            }

            // KISS safeguard: prevent absurdly large ranges (> 365 days)
            if (startUtc.HasValue && endUtc.HasValue)
            {
                var maxRangeDays = 365;
                if ((endUtc.Value - startUtc.Value).TotalDays > maxRangeDays)
                {
                    return Results.BadRequest(new { error = $"Time range cannot exceed {maxRangeDays} days." });
                }
            }

            // Build query with filters
            var query = db.Bookings.AsNoTracking();

            // Date range filtering (overlap detection)
            if (startUtc.HasValue && endUtc.HasValue)
            {
                // Both provided: return bookings overlapping the range
                query = query.Where(b => startUtc.Value < b.EndUtc && endUtc.Value > b.StartUtc);
            }
            else if (startUtc.HasValue)
            {
                // Only start provided: bookings ending after start
                query = query.Where(b => b.EndUtc > startUtc.Value);
            }
            else if (endUtc.HasValue)
            {
                // Only end provided: bookings starting before end
                query = query.Where(b => b.StartUtc < endUtc.Value);
            }

            // Client filter
            if (clientId.HasValue)
            {
                query = query.Where(b => b.ClientId == clientId.Value);
            }

            // Status filter (default: exclude cancelled)
            var includeCancelledBookings = includeCancelled ?? false;
            if (!includeCancelledBookings)
            {
                query = query.Where(b => !b.IsCancelled);
            }

            // Sort: StartUtc ascending, then Title
            var bookings = query
                .OrderBy(b => b.StartUtc)
                .ThenBy(b => b.Title)
                .Select(b => new
                {
                    b.Id,
                    b.ClientId,
                    b.Title,
                    b.StartUtc,
                    b.EndUtc,
                    b.AttendeeCount,
                    b.IsCancelled,
                    b.TotalAmount,
                    b.SpaceConfigurationId,
                    b.Status,
                    b.CancelledUtc,
                    b.CancelReason,
                    b.CreatedByUserId,
                    b.ConfirmedByUserId,
                    b.CancelledByUserId
                })
                .ToList();

            // Get space IDs for all bookings in one query
            var bookingIds = bookings.Select(b => b.Id).ToList();
            var bookingSpaces = db.BookingSpaces
                .AsNoTracking()
                .Where(bs => bookingIds.Contains(bs.BookingId))
                .ToList();

            var spaceIdsByBooking = bookingSpaces
                .GroupBy(bs => bs.BookingId)
                .ToDictionary(g => g.Key, g => g.Select(bs => bs.SpaceId).ToList());

            var response = bookings.Select(b => new BookingResponse(
                b.Id,
                b.ClientId,
                b.Title,
                b.StartUtc,
                b.EndUtc,
                b.AttendeeCount,
                b.IsCancelled,
                spaceIdsByBooking.TryGetValue(b.Id, out var spaceIds) ? spaceIds : new List<Guid>(),
                b.TotalAmount,
                b.SpaceConfigurationId,
                b.Status,
                b.CancelledUtc,
                b.CancelReason,
                b.CreatedByUserId,
                b.ConfirmedByUserId,
                b.CancelledByUserId));

            return Results.Ok(response);
        })
        .RequireAuthorization();

        // GET /{companySlug}/bookings/{id} - Get a specific booking with space IDs (requires auth + membership)
        group.MapGet("/bookings/{id:guid}", (ApplicationDbContext db, ITenantContext tenantContext, ClaimsPrincipal user, Guid id) =>
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

            var booking = db.Bookings
                .AsNoTracking()
                .Select(b => new { b.Id, b.ClientId, b.Title, b.StartUtc, b.EndUtc, b.AttendeeCount, b.IsCancelled, b.CompanyId, b.TotalAmount, b.SpaceConfigurationId, b.Status, b.CancelledUtc, b.CancelReason, b.CreatedByUserId, b.ConfirmedByUserId, b.CancelledByUserId })
                .FirstOrDefault(b => b.Id == id && b.CompanyId == tenant.CompanyId);

            if (booking is null)
            {
                return Results.NotFound();
            }

            var spaceIds = db.BookingSpaces
                .AsNoTracking()
                .Where(bs => bs.BookingId == id)
                .Select(bs => bs.SpaceId)
                .ToList();

            return Results.Ok(new BookingResponse(
                booking.Id,
                booking.ClientId,
                booking.Title,
                booking.StartUtc,
                booking.EndUtc,
                booking.AttendeeCount,
                booking.IsCancelled,
                spaceIds,
                booking.TotalAmount,
                booking.SpaceConfigurationId,
                booking.Status,
                booking.CancelledUtc,
                booking.CancelReason, booking.CreatedByUserId, booking.ConfirmedByUserId, booking.CancelledByUserId));
        })
        .RequireAuthorization();

        // GET /{companySlug}/bookings/{id}/details - Get detailed booking info including client name and space names (requires auth + membership)
        group.MapGet("/bookings/{id:guid}/details", (ApplicationDbContext db, ITenantContext tenantContext, ClaimsPrincipal user, Guid id) =>
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

            // Load booking (tenant-filtered)
            var booking = db.Bookings
                .AsNoTracking()
                .Select(b => new { b.Id, b.ClientId, b.Title, b.StartUtc, b.EndUtc, b.AttendeeCount, b.IsCancelled, b.CompanyId, b.TotalAmount, b.SpaceConfigurationId, b.Status, b.CancelledUtc, b.CancelReason, b.CreatedByUserId, b.ConfirmedByUserId, b.CancelledByUserId })
                .FirstOrDefault(b => b.Id == id && b.CompanyId == tenant.CompanyId);

            if (booking is null)
            {
                return Results.NotFound();
            }

            // Load client name (tenant-filtered)
            var clientName = db.Clients
                .AsNoTracking()
                .Where(c => c.Id == booking.ClientId && c.CompanyId == tenant.CompanyId)
                .Select(c => c.Name)
                .FirstOrDefault();

            if (clientName is null)
            {
                // This shouldn't happen due to FK constraints, but handle gracefully
                return Results.Problem("Booking client not found.", statusCode: 500);
            }

            // Load attached spaces (Ids + Names) via BookingSpaces join, ordered by Name
            var spaces = db.BookingSpaces
                .AsNoTracking()
                .Where(bs => bs.BookingId == id)
                .Join(
                    db.Spaces.AsNoTracking().Where(s => s.CompanyId == tenant.CompanyId),
                    bs => bs.SpaceId,
                    s => s.Id,
                    (bs, s) => new SpaceSummary(s.Id, s.Name))
                .OrderBy(s => s.Name)
                .ToList();

            var response = new BookingDetailsResponse(
                booking.Id,
                booking.ClientId,
                clientName,
                booking.Title,
                booking.StartUtc,
                booking.EndUtc,
                booking.AttendeeCount,
                booking.IsCancelled,
                booking.TotalAmount,
                booking.SpaceConfigurationId,
                spaces,
                booking.Status,
                booking.CancelledUtc,
                booking.CancelReason,
                booking.CreatedByUserId,
                booking.ConfirmedByUserId,
                booking.CancelledByUserId);

            return Results.Ok(response);
        })
        .RequireAuthorization();

        // POST /{companySlug}/bookings - Creates a new booking for current tenant (requires auth + Manager/Admin/Owner)
        group.MapPost("/bookings", async (ApplicationDbContext db, ITenantContext tenantContext, ClaimsPrincipal user, CreateBookingRequest request) =>
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
            if (string.IsNullOrWhiteSpace(request.Title))
            {
                return Results.BadRequest(new { error = "Title is required." });
            }
            if (request.Title.Length > 200)
            {
                return Results.BadRequest(new { error = "Title cannot exceed 200 characters." });
            }
            if (request.StartUtc >= request.EndUtc)
            {
                return Results.BadRequest(new { error = "Start time must be before end time." });
            }
            if (request.AttendeeCount < 0)
            {
                return Results.BadRequest(new { error = "Attendee count cannot be negative." });
            }

            // Verify client exists and belongs to this tenant
            var clientExists = db.Clients
                .AsNoTracking()
                .Any(c => c.Id == request.ClientId && c.CompanyId == tenant.CompanyId);

            if (!clientExists)
            {
                return Results.BadRequest(new { error = "Client not found or does not belong to this tenant." });
            }

            // Validate SpaceConfigurationId if provided
            if (request.SpaceConfigurationId.HasValue)
            {
                var configExists = db.SpaceConfigurations
                    .AsNoTracking()
                    .Any(sc => sc.Id == request.SpaceConfigurationId.Value && sc.CompanyId == tenant.CompanyId && sc.IsActive);

                if (!configExists)
                {
                    return Results.BadRequest(new { error = "Space configuration not found, does not belong to this tenant, or is not active." });
                }
            }

            // Validate minimum booking duration if SpaceConfiguration has override
            var (durationOk, minMinutes) = await EndpointHelpers.ValidateMinBookingDurationAsync(
                request.SpaceConfigurationId,
                request.StartUtc,
                request.EndUtc,
                db);

            if (!durationOk)
            {
                return Results.BadRequest(new {
                    error = "Booking duration is below the minimum allowed.",
                    minMinutes = minMinutes
                });
            }

            var booking = new Booking(tenant.CompanyId, request.ClientId, request.Title, request.StartUtc, request.EndUtc, request.AttendeeCount);

            // Set CreatedByUserId from current user
            booking.SetCreatedBy(userId);

            // Set SpaceConfigurationId if provided via type-safe setter
            if (request.SpaceConfigurationId.HasValue)
            {
                booking.SetSpaceConfigurationId(request.SpaceConfigurationId.Value);
            }
            
            db.Bookings.Add(booking);
            db.SaveChanges();

            return Results.Created(
                $"/{tenant.CompanySlug}/bookings/{booking.Id}",
                new BookingResponse(booking.Id, booking.ClientId, booking.Title, booking.StartUtc, booking.EndUtc, booking.AttendeeCount, booking.IsCancelled, new List<Guid>(), booking.TotalAmount, booking.SpaceConfigurationId, booking.Status, booking.CancelledUtc, booking.CancelReason, booking.CreatedByUserId, booking.ConfirmedByUserId, booking.CancelledByUserId));
        })
        .RequireAuthorization();

        // POST /{companySlug}/bookings/with-spaces - Creates a new booking with spaces atomically (requires auth + Manager/Admin/Owner)
        group.MapPost("/bookings/with-spaces", async (ApplicationDbContext db, ITenantContext tenantContext, ClaimsPrincipal user, CreateBookingWithSpacesRequest request) =>
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

            // 1) Validate basic fields
            if (string.IsNullOrWhiteSpace(request.Title))
            {
                return Results.BadRequest(new { error = "Title is required." });
            }
            if (request.Title.Length > 200)
            {
                return Results.BadRequest(new { error = "Title cannot exceed 200 characters." });
            }
            if (request.StartUtc >= request.EndUtc)
            {
                return Results.BadRequest(new { error = "Start time must be before end time." });
            }
            if (request.AttendeeCount < 0)
            {
                return Results.BadRequest(new { error = "Attendee count cannot be negative." });
            }

            // 2) Validate Client exists in tenant
            var clientExists = db.Clients
                .AsNoTracking()
                .Any(c => c.Id == request.ClientId && c.CompanyId == tenant.CompanyId);

            if (!clientExists)
            {
                return Results.BadRequest(new { error = "Client not found or does not belong to this tenant." });
            }

            // 3) Validate SpaceConfigurationId if provided
            if (request.SpaceConfigurationId.HasValue)
            {
                var configExists = db.SpaceConfigurations
                    .AsNoTracking()
                    .Any(sc => sc.Id == request.SpaceConfigurationId.Value && sc.CompanyId == tenant.CompanyId && sc.IsActive);

                if (!configExists)
                {
                    return Results.BadRequest(new { error = "Space configuration not found, does not belong to this tenant, or is not active." });
                }
            }

            // 4) Validate minimum booking duration
            var (durationOk, minMinutes) = await EndpointHelpers.ValidateMinBookingDurationAsync(
                request.SpaceConfigurationId,
                request.StartUtc,
                request.EndUtc,
                db);

            if (!durationOk)
            {
                return Results.BadRequest(new {
                    error = "Booking duration is below the minimum allowed.",
                    minMinutes = minMinutes
                });
            }

            // 5) Validate SpaceIds
            if (request.SpaceIds.Count == 0)
            {
                return Results.BadRequest(new { error = "SpaceIds is required and must not be empty." });
            }

            // Deduplicate SpaceIds
            var dedupedSpaceIds = request.SpaceIds.ToHashSet();

            // Ensure all spaces exist in tenant
            var existingSpaceIds = db.Spaces
                .AsNoTracking()
                .Where(s => dedupedSpaceIds.Contains(s.Id) && s.CompanyId == tenant.CompanyId)
                .Select(s => s.Id)
                .ToList();

            if (existingSpaceIds.Count != dedupedSpaceIds.Count)
            {
                var missingIds = dedupedSpaceIds.Except(existingSpaceIds).ToList();
                return Results.BadRequest(new { error = "One or more space IDs are invalid or do not belong to this tenant.", missingSpaceIds = missingIds });
            }

            // 6) Conflict detection
            var (conflictingBookingIds, conflictingSpaceIds) = EndpointHelpers.FindConflictingBookings(
                db,
                tenant.CompanyId,
                request.StartUtc,
                request.EndUtc,
                dedupedSpaceIds,
                null); // No booking to exclude for create

            if (conflictingBookingIds.Count > 0)
            {
                return Results.Conflict(new BookingConflictResponse(
                    "Booking conflicts with existing bookings.",
                    conflictingBookingIds,
                    conflictingSpaceIds));
            }

            // 7) Create Booking
            var booking = new Booking(tenant.CompanyId, request.ClientId, request.Title, request.StartUtc, request.EndUtc, request.AttendeeCount);

            // Set CreatedByUserId from current user
            booking.SetCreatedBy(userId);

            // Set SpaceConfigurationId if provided
            if (request.SpaceConfigurationId.HasValue)
            {
                booking.SetSpaceConfigurationId(request.SpaceConfigurationId.Value);
            }

            db.Bookings.Add(booking);

            // 8) Create BookingSpaces rows
            foreach (var spaceId in dedupedSpaceIds)
            {
                db.BookingSpaces.Add(new BookingSpace
                {
                    BookingId = booking.Id,
                    SpaceId = spaceId
                });
            }

            // 9) Calculate TotalAmount
            // Load space hourly rates
            var spaceHourlyRates = db.Spaces
                .AsNoTracking()
                .Where(s => dedupedSpaceIds.Contains(s.Id))
                .Select(s => s.HourlyRate)
                .ToList();

            // Load space configuration override rate if set
            decimal? overrideRate = null;
            if (request.SpaceConfigurationId.HasValue)
            {
                overrideRate = db.SpaceConfigurations
                    .AsNoTracking()
                    .Where(sc => sc.Id == request.SpaceConfigurationId.Value)
                    .Select(sc => sc.HourlyRateOverride)
                    .FirstOrDefault();
            }

            var totalAmount = EndpointHelpers.CalculateBookingTotal(request.StartUtc, request.EndUtc, spaceHourlyRates, overrideRate);
            booking.SetTotalAmount(totalAmount);

            // 10) SaveChanges (atomic)
            await db.SaveChangesAsync();

            // 11) Return 201 Created with BookingResponse
            return Results.Created(
                $"/{tenant.CompanySlug}/bookings/{booking.Id}",
                new BookingResponse(
                    booking.Id,
                    booking.ClientId,
                    booking.Title,
                    booking.StartUtc,
                    booking.EndUtc,
                    booking.AttendeeCount,
                    booking.IsCancelled,
                    dedupedSpaceIds.ToList(),
                    booking.TotalAmount,
                    booking.SpaceConfigurationId,
                    booking.Status,
                    booking.CancelledUtc,
                    booking.CancelReason, booking.CreatedByUserId, booking.ConfirmedByUserId, booking.CancelledByUserId));
        })
        .RequireAuthorization();

        // PUT /{companySlug}/bookings/{id} - Update booking details (requires auth + Manager/Admin/Owner)
        group.MapPut("/bookings/{id:guid}", async (ApplicationDbContext db, ITenantContext tenantContext, ClaimsPrincipal user, Guid id, UpdateBookingRequest request) =>
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

            // Load booking
            var booking = db.Bookings
                .FirstOrDefault(b => b.Id == id && b.CompanyId == tenant.CompanyId);

            if (booking is null)
            {
                return Results.NotFound();
            }

            // Reject updates to cancelled bookings
            if (booking.IsCancelled)
            {
                return Results.BadRequest(new { error = "Cancelled booking cannot be modified." });
            }

            // Reject updates to confirmed bookings
            if (booking.Status == BookingStatus.Confirmed)
            {
                return Results.BadRequest(new { error = "Confirmed booking cannot be modified." });
            }

            // Validate request (same as create)
            if (string.IsNullOrWhiteSpace(request.Title))
            {
                return Results.BadRequest(new { error = "Title is required." });
            }
            if (request.Title.Length > 200)
            {
                return Results.BadRequest(new { error = "Title cannot exceed 200 characters." });
            }
            if (request.StartUtc >= request.EndUtc)
            {
                return Results.BadRequest(new { error = "Start time must be before end time." });
            }
            if (request.AttendeeCount < 0)
            {
                return Results.BadRequest(new { error = "Attendee count cannot be negative." });
            }

            // Validate SpaceConfigurationId if provided
            if (request.SpaceConfigurationId.HasValue)
            {
                var configExists = db.SpaceConfigurations
                    .AsNoTracking()
                    .Any(sc => sc.Id == request.SpaceConfigurationId.Value && sc.CompanyId == tenant.CompanyId && sc.IsActive);

                if (!configExists)
                {
                    return Results.BadRequest(new { error = "Space configuration not found, does not belong to this tenant, or is not active." });
                }
            }

            // Validate minimum booking duration if SpaceConfiguration has override
            // Use the request's SpaceConfigurationId (may be changing) and request's time values
            var (durationOk, minMinutes) = await EndpointHelpers.ValidateMinBookingDurationAsync(
                request.SpaceConfigurationId,
                request.StartUtc,
                request.EndUtc,
                db);

            if (!durationOk)
            {
                return Results.BadRequest(new {
                    error = "Booking duration is below the minimum allowed.",
                    minMinutes = minMinutes
                });
            }

            // Get existing spaces attached to this booking
            var existingSpaceIds = db.BookingSpaces
                .AsNoTracking()
                .Where(bs => bs.BookingId == id)
                .Select(bs => bs.SpaceId)
                .ToList();

            // Run conflict detection if booking has spaces
            if (existingSpaceIds.Count > 0)
            {
                var (conflictingBookingIds, conflictingSpaceIds) = EndpointHelpers.FindConflictingBookings(
                    db,
                    tenant.CompanyId,
                    request.StartUtc,
                    request.EndUtc,
                    existingSpaceIds,
                    id); // Exclude current booking

                if (conflictingBookingIds.Count > 0)
                {
                    return Results.Conflict(new BookingConflictResponse(
                        "Booking conflicts with existing bookings.",
                        conflictingBookingIds,
                        conflictingSpaceIds));
                }
            }

            // Update booking fields using type-safe domain method
            booking.UpdateDetails(request.Title, request.StartUtc, request.EndUtc, request.AttendeeCount);

            // Update SpaceConfigurationId via type-safe setter
            booking.SetSpaceConfigurationId(request.SpaceConfigurationId);

            // Recalculate TotalAmount if booking has spaces
            if (existingSpaceIds.Count > 0)
            {
                // Load space hourly rates
                var spaceHourlyRates = db.Spaces
                    .AsNoTracking()
                    .Where(s => existingSpaceIds.Contains(s.Id))
                    .Select(s => s.HourlyRate)
                    .ToList();

                // Load space configuration override rate if set
                decimal? overrideRate = null;
                if (request.SpaceConfigurationId.HasValue)
                {
                    overrideRate = db.SpaceConfigurations
                        .AsNoTracking()
                        .Where(sc => sc.Id == request.SpaceConfigurationId.Value)
                        .Select(sc => sc.HourlyRateOverride)
                        .FirstOrDefault();
                }

                var totalAmount = EndpointHelpers.CalculateBookingTotal(request.StartUtc, request.EndUtc, spaceHourlyRates, overrideRate);
                booking.SetTotalAmount(totalAmount);
            }

            await db.SaveChangesAsync();

            // Return updated space IDs
            var updatedSpaceIds = db.BookingSpaces
                .AsNoTracking()
                .Where(bs => bs.BookingId == id)
                .Select(bs => bs.SpaceId)
                .ToList();

            return Results.Ok(new BookingResponse(
                booking.Id,
                booking.ClientId,
                booking.Title,
                booking.StartUtc,
                booking.EndUtc,
                booking.AttendeeCount,
                booking.IsCancelled,
                updatedSpaceIds,
                booking.TotalAmount,
                booking.SpaceConfigurationId,
                booking.Status,
                booking.CancelledUtc,
                booking.CancelReason, booking.CreatedByUserId, booking.ConfirmedByUserId, booking.CancelledByUserId));
        })
        .RequireAuthorization();

        // PUT /{companySlug}/bookings/{id}/spaces - Replace spaces for a booking (requires auth + Manager/Admin/Owner)
        group.MapPut("/bookings/{id:guid}/spaces", async (ApplicationDbContext db, ITenantContext tenantContext, ClaimsPrincipal user, Guid id, SetBookingSpacesRequest request) =>
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

            // Load booking (tenant-filtered)
            var booking = db.Bookings
                .FirstOrDefault(b => b.Id == id && b.CompanyId == tenant.CompanyId);

            if (booking is null)
            {
                return Results.NotFound();
            }

            // Reject updates to cancelled bookings
            if (booking.IsCancelled)
            {
                return Results.BadRequest(new { error = "Cancelled booking cannot be modified." });
            }

            // Reject updates to confirmed bookings
            if (booking.Status == BookingStatus.Confirmed)
            {
                return Results.BadRequest(new { error = "Confirmed booking cannot be modified." });
            }

            // If requested SpaceIds is empty -> clear and return (no conflict check needed)
            if (request.SpaceIds.Count == 0)
            {
                // Remove existing associations
                var existingAssociations = db.BookingSpaces
                    .Where(bs => bs.BookingId == id)
                    .ToList();

                db.BookingSpaces.RemoveRange(existingAssociations);

                // Set TotalAmount to 0 when clearing spaces
                booking.SetTotalAmount(0);

                await db.SaveChangesAsync();

                return Results.Ok(new BookingResponse(
                    booking.Id,
                    booking.ClientId,
                    booking.Title,
                    booking.StartUtc,
                    booking.EndUtc,
                    booking.AttendeeCount,
                    booking.IsCancelled,
                    new List<Guid>(),
                    booking.TotalAmount,
                    booking.SpaceConfigurationId,
                    booking.Status,
                    booking.CancelledUtc,
                    booking.CancelReason, booking.CreatedByUserId, booking.ConfirmedByUserId, booking.CancelledByUserId));
            }

            // Validate that all space IDs exist and belong to this tenant
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

            // Run conflict detection using booking's time range and requested spaces
            var (conflictingBookingIds, conflictingSpaceIds) = EndpointHelpers.FindConflictingBookings(
                db,
                tenant.CompanyId,
                booking.StartUtc,
                booking.EndUtc,
                requestedSpaceIds,
                id); // Exclude current booking

            if (conflictingBookingIds.Count > 0)
            {
                return Results.Conflict(new BookingConflictResponse(
                    "Booking conflicts with existing bookings.",
                    conflictingBookingIds,
                    conflictingSpaceIds));
            }

            // Remove existing associations
            var existingAssoc = db.BookingSpaces
                .Where(bs => bs.BookingId == id)
                .ToList();

            db.BookingSpaces.RemoveRange(existingAssoc);

            // Add new associations
            foreach (var spaceId in request.SpaceIds)
            {
                db.BookingSpaces.Add(new BookingSpace
                {
                    BookingId = id,
                    SpaceId = spaceId
                });
            }

            // Load space hourly rates and calculate TotalAmount
            var spaceHourlyRates = db.Spaces
                .AsNoTracking()
                .Where(s => requestedSpaceIds.Contains(s.Id))
                .Select(s => s.HourlyRate)
                .ToList();

            // Load space configuration override rate if set
            decimal? overrideRate = null;
            if (booking.SpaceConfigurationId.HasValue)
            {
                overrideRate = db.SpaceConfigurations
                    .AsNoTracking()
                    .Where(sc => sc.Id == booking.SpaceConfigurationId.Value)
                    .Select(sc => sc.HourlyRateOverride)
                    .FirstOrDefault();
            }

            var totalAmount = EndpointHelpers.CalculateBookingTotal(booking.StartUtc, booking.EndUtc, spaceHourlyRates, overrideRate);
            booking.SetTotalAmount(totalAmount);

            await db.SaveChangesAsync();

            // Return updated space IDs
            var updatedSpaceIds = db.BookingSpaces
                .AsNoTracking()
                .Where(bs => bs.BookingId == id)
                .Select(bs => bs.SpaceId)
                .ToList();

            return Results.Ok(new BookingResponse(
                booking.Id,
                booking.ClientId,
                booking.Title,
                booking.StartUtc,
                booking.EndUtc,
                booking.AttendeeCount,
                booking.IsCancelled,
                updatedSpaceIds,
                booking.TotalAmount,
                booking.SpaceConfigurationId,
                booking.Status,
                booking.CancelledUtc,
                booking.CancelReason, booking.CreatedByUserId, booking.ConfirmedByUserId, booking.CancelledByUserId));
        })
        .RequireAuthorization();

        // PUT /{companySlug}/bookings/{id}/with-spaces - Update booking fields and replace spaces atomically (requires auth + Manager/Admin/Owner)
        group.MapPut("/bookings/{id:guid}/with-spaces", async (ApplicationDbContext db, ITenantContext tenantContext, ClaimsPrincipal user, Guid id, UpdateBookingWithSpacesRequest request) =>
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

            // 1) Load booking (tenant-filtered)
            var booking = db.Bookings
                .FirstOrDefault(b => b.Id == id && b.CompanyId == tenant.CompanyId);

            if (booking is null)
            {
                return Results.NotFound();
            }

            // Reject updates to cancelled bookings
            if (booking.IsCancelled)
            {
                return Results.BadRequest(new { error = "Cancelled booking cannot be modified." });
            }

            // Reject updates to confirmed bookings
            if (booking.Status == BookingStatus.Confirmed)
            {
                return Results.BadRequest(new { error = "Confirmed booking cannot be modified." });
            }

            // 2) Validate basic fields
            if (string.IsNullOrWhiteSpace(request.Title))
            {
                return Results.BadRequest(new { error = "Title is required." });
            }
            if (request.Title.Length > 200)
            {
                return Results.BadRequest(new { error = "Title cannot exceed 200 characters." });
            }
            if (request.StartUtc >= request.EndUtc)
            {
                return Results.BadRequest(new { error = "Start time must be before end time." });
            }
            if (request.AttendeeCount < 0)
            {
                return Results.BadRequest(new { error = "Attendee count cannot be negative." });
            }

            // 3) Validate SpaceConfigurationId if provided
            if (request.SpaceConfigurationId.HasValue)
            {
                var configExists = db.SpaceConfigurations
                    .AsNoTracking()
                    .Any(sc => sc.Id == request.SpaceConfigurationId.Value && sc.CompanyId == tenant.CompanyId && sc.IsActive);

                if (!configExists)
                {
                    return Results.BadRequest(new { error = "Space configuration not found, does not belong to this tenant, or is not active." });
                }
            }

            // 4) Validate minimum booking duration
            var (durationOk, minMinutes) = await EndpointHelpers.ValidateMinBookingDurationAsync(
                request.SpaceConfigurationId,
                request.StartUtc,
                request.EndUtc,
                db);

            if (!durationOk)
            {
                return Results.BadRequest(new {
                    error = "Booking duration is below the minimum allowed.",
                    minMinutes = minMinutes
                });
            }

            // 5) Validate SpaceIds
            if (request.SpaceIds.Count == 0)
            {
                return Results.BadRequest(new { error = "SpaceIds is required and must not be empty." });
            }

            // Deduplicate SpaceIds
            var dedupedSpaceIds = request.SpaceIds.ToHashSet();

            // Ensure all spaces exist in tenant
            var existingSpaceIds = db.Spaces
                .AsNoTracking()
                .Where(s => dedupedSpaceIds.Contains(s.Id) && s.CompanyId == tenant.CompanyId)
                .Select(s => s.Id)
                .ToList();

            if (existingSpaceIds.Count != dedupedSpaceIds.Count)
            {
                var missingIds = dedupedSpaceIds.Except(existingSpaceIds).ToList();
                return Results.BadRequest(new { error = "One or more space IDs are invalid or do not belong to this tenant.", missingSpaceIds = missingIds });
            }

            // 6) Conflict detection (exclude current booking)
            var (conflictingBookingIds, conflictingSpaceIds) = EndpointHelpers.FindConflictingBookings(
                db,
                tenant.CompanyId,
                request.StartUtc,
                request.EndUtc,
                dedupedSpaceIds,
                id); // Exclude current booking

            if (conflictingBookingIds.Count > 0)
            {
                return Results.Conflict(new BookingConflictResponse(
                    "Booking conflicts with existing bookings.",
                    conflictingBookingIds,
                    conflictingSpaceIds));
            }

            // 7) Replace spaces: load existing join rows, compute toRemove/toAdd
            var existingBookingSpaceIds = db.BookingSpaces
                .Where(bs => bs.BookingId == id)
                .Select(bs => bs.SpaceId)
                .ToHashSet();

            var toRemove = existingBookingSpaceIds.Except(dedupedSpaceIds).ToList();
            var toAdd = dedupedSpaceIds.Except(existingBookingSpaceIds).ToList();

            // Remove associations no longer needed
            if (toRemove.Count > 0)
            {
                var removeAssociations = db.BookingSpaces
                    .Where(bs => bs.BookingId == id && toRemove.Contains(bs.SpaceId))
                    .ToList();
                db.BookingSpaces.RemoveRange(removeAssociations);
            }

            // Add new associations
            foreach (var spaceId in toAdd)
            {
                db.BookingSpaces.Add(new BookingSpace
                {
                    BookingId = id,
                    SpaceId = spaceId
                });
            }

            // 8) Update booking fields using type-safe domain method
            booking.UpdateDetails(request.Title, request.StartUtc, request.EndUtc, request.AttendeeCount);

            // Update SpaceConfigurationId via type-safe setter
            booking.SetSpaceConfigurationId(request.SpaceConfigurationId);

            // 9) Recalculate TotalAmount
            // Load hourly rates for requested spaces
            var spaceHourlyRates = db.Spaces
                .AsNoTracking()
                .Where(s => dedupedSpaceIds.Contains(s.Id))
                .Select(s => s.HourlyRate)
                .ToList();

            // Load override hourly rate from SpaceConfiguration if set
            decimal? overrideRate = null;
            if (request.SpaceConfigurationId.HasValue)
            {
                overrideRate = db.SpaceConfigurations
                    .AsNoTracking()
                    .Where(sc => sc.Id == request.SpaceConfigurationId.Value)
                    .Select(sc => sc.HourlyRateOverride)
                    .FirstOrDefault();
            }

            var totalAmount = EndpointHelpers.CalculateBookingTotal(request.StartUtc, request.EndUtc, spaceHourlyRates, overrideRate);
            booking.SetTotalAmount(totalAmount);

            // 10) SaveChangesAsync (single call for atomicity)
            await db.SaveChangesAsync();

            // 11) Return 200 OK with BookingResponse
            return Results.Ok(new BookingResponse(
                booking.Id,
                booking.ClientId,
                booking.Title,
                booking.StartUtc,
                booking.EndUtc,
                booking.AttendeeCount,
                booking.IsCancelled,
                dedupedSpaceIds.ToList(),
                booking.TotalAmount,
                booking.SpaceConfigurationId,
                booking.Status,
                booking.CancelledUtc,
                booking.CancelReason, booking.CreatedByUserId, booking.ConfirmedByUserId, booking.CancelledByUserId));
        })
        .RequireAuthorization();

        // POST /{companySlug}/bookings/{id}/confirm - Confirm a booking (requires auth + Manager/Admin/Owner)
        group.MapPost("/bookings/{id:guid}/confirm", (ApplicationDbContext db, ITenantContext tenantContext, ClaimsPrincipal user, Guid id) =>
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

            var booking = db.Bookings
                .FirstOrDefault(b => b.Id == id && b.CompanyId == tenant.CompanyId);

            if (booking is null)
            {
                return Results.NotFound();
            }

            // Cannot confirm cancelled bookings
            if (booking.IsCancelled)
            {
                return Results.BadRequest(new { error = "Cancelled booking cannot be confirmed." });
            }

            // Idempotent: if already confirmed, return 204
            if (booking.Status == BookingStatus.Confirmed)
            {
                return Results.NoContent();
            }

            booking.Confirm(userId);
            db.SaveChanges();

            return Results.NoContent();
        })
        .RequireAuthorization();

        // DELETE /{companySlug}/bookings/{id} - Soft cancel a booking (requires auth + Manager/Admin/Owner)
        group.MapDelete("/bookings/{id:guid}", (ApplicationDbContext db, ITenantContext tenantContext, ClaimsPrincipal user, Guid id, string? reason = null) =>
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

            var booking = db.Bookings
                .FirstOrDefault(b => b.Id == id && b.CompanyId == tenant.CompanyId);

            if (booking is null)
            {
                return Results.NotFound();
            }

            // Idempotent: if already cancelled, just return 204 (do not overwrite existing metadata)
            if (booking.IsCancelled)
            {
                return Results.NoContent();
            }

            booking.Cancel(reason, DateTime.UtcNow, userId);
            db.SaveChanges();

            return Results.NoContent();
        })
        .RequireAuthorization();

        return group;
    }
}
