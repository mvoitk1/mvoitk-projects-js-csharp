using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using VenuePlatform.DAL.Persistence;

namespace VenuePlatform.Web.Endpoints;

/// <summary>
/// Shared helper methods for endpoint handlers. Logic is byte-for-byte equivalent to original Program.cs helpers.
/// </summary>
internal static class EndpointHelpers
{
    /// <summary>
    /// Extracts the user ID from the "sub" claim. Returns Guid.Empty if invalid/missing.
    /// </summary>
    internal static Guid GetUserIdFromClaims(ClaimsPrincipal user)
    {
        var userIdClaim = user.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Guid.Empty;
        }
        return userId;
    }

    /// <summary>
    /// Finds conflicting bookings for space-level conflict detection.
    /// Returns conflicting booking IDs and space IDs.
    /// </summary>
    internal static (IReadOnlyList<Guid> BookingIds, IReadOnlyList<Guid> SpaceIds) FindConflictingBookings(
        ApplicationDbContext db,
        Guid companyId,
        DateTime startUtc,
        DateTime endUtc,
        IEnumerable<Guid> targetSpaceIds,
        Guid? excludeBookingId)
    {
        var spaceIdSet = targetSpaceIds.ToHashSet();
        if (spaceIdSet.Count == 0)
        {
            return (Array.Empty<Guid>(), Array.Empty<Guid>());
        }

        // Query: overlapping bookings sharing any of the target spaces
        // Overlap rule: newStart < existingEnd && newEnd > existingStart
        var query = db.Bookings
            .AsNoTracking()
            .Where(b => b.CompanyId == companyId)
            .Where(b => !b.IsCancelled)
            .Where(b => b.StartUtc < endUtc && b.EndUtc > startUtc)
            .Where(b => db.BookingSpaces.AsNoTracking().Any(bs => bs.BookingId == b.Id && spaceIdSet.Contains(bs.SpaceId)));

        if (excludeBookingId.HasValue)
        {
            query = query.Where(b => b.Id != excludeBookingId.Value);
        }

        var conflictingBookings = query
            .Select(b => new { b.Id })
            .ToList();

        if (conflictingBookings.Count == 0)
        {
            return (Array.Empty<Guid>(), Array.Empty<Guid>());
        }

        var conflictingBookingIds = conflictingBookings.Select(b => b.Id).ToList();

        // Find which specific spaces conflict
        var conflictingSpaceIds = db.BookingSpaces
            .AsNoTracking()
            .Where(bs => conflictingBookingIds.Contains(bs.BookingId) && spaceIdSet.Contains(bs.SpaceId))
            .Select(bs => bs.SpaceId)
            .Distinct()
            .ToList();

        return (conflictingBookingIds, conflictingSpaceIds);
    }

    /// <summary>
    /// Validates minimum booking duration against SpaceConfiguration override.
    /// Returns (ok, minMinutes) - if ok is false, minMinutes contains the required minimum.
    /// </summary>
    internal static async Task<(bool ok, int? minMinutes)> ValidateMinBookingDurationAsync(
        Guid? spaceConfigurationId,
        DateTime startUtc,
        DateTime endUtc,
        ApplicationDbContext db)
    {
        // If no SpaceConfiguration selected, no minimum duration rule applies
        if (spaceConfigurationId == null)
        {
            return (true, null);
        }

        // Load SpaceConfiguration using existing tenant filtering
        var config = await db.SpaceConfigurations
            .AsNoTracking()
            .FirstOrDefaultAsync(sc => sc.Id == spaceConfigurationId.Value);

        // If config not found or override not set, no validation
        if (config?.MinBookingMinutesOverride == null || config.MinBookingMinutesOverride <= 0)
        {
            return (true, null);
        }

        var minMinutes = config.MinBookingMinutesOverride.Value;
        var durationMinutes = (endUtc - startUtc).TotalMinutes;

        // Validate actual duration (not billing duration)
        if (durationMinutes < minMinutes)
        {
            return (false, minMinutes);
        }

        return (true, null);
    }

    /// <summary>
    /// Calculates booking total amount based on duration and space rates.
    /// - Minutes rounded UP to nearest 15 (ceiling)
    /// - Convert to hours as decimal
    /// - If overrideRate is set, use that for ALL spaces
    /// - Otherwise, use each space's hourly rate
    /// - Round to 2 decimals using MidpointRounding.AwayFromZero
    /// </summary>
    internal static decimal CalculateBookingTotal(
        DateTime startUtc,
        DateTime endUtc,
        IReadOnlyList<decimal> spaceHourlyRates,
        decimal? bookingWideOverrideHourlyRate = null)
    {
        var durationMinutes = (endUtc - startUtc).TotalMinutes;
        if (durationMinutes <= 0 || spaceHourlyRates.Count == 0)
            return 0;

        // Round UP to nearest 15 minutes
        var billingUnits = (int)Math.Ceiling(durationMinutes / 15.0);
        var billedMinutes = billingUnits * 15;
        var billedHours = billedMinutes / 60m;

        decimal total;

        // If override rate is provided, use it for all spaces
        if (bookingWideOverrideHourlyRate.HasValue)
        {
            total = bookingWideOverrideHourlyRate.Value * billedHours * spaceHourlyRates.Count;
        }
        else
        {
            // Sum rates * hours per space
            total = spaceHourlyRates.Sum(rate => rate * billedHours);
        }

        // Round to 2 decimals
        return Math.Round(total, 2, MidpointRounding.AwayFromZero);
    }
}
