using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using VenuePlatform.BLL.Limits;
using VenuePlatform.BLL.Tenancy;
using VenuePlatform.Contracts.Auth;
using VenuePlatform.Contracts.Reports;
using VenuePlatform.DAL.Persistence;

namespace VenuePlatform.Web.Endpoints;

/// <summary>
/// Reporting endpoints (tenant-scoped).
/// </summary>
public static class ReportEndpoints
{
    public static RouteGroupBuilder MapReportEndpoints(this RouteGroupBuilder group)
    {
        // GET /{companySlug}/reports/revenue - Revenue summary for date range (requires auth + any membership)
        group.MapGet("/reports/revenue", async (ApplicationDbContext db, ITenantContext tenantContext, ClaimsPrincipal user, DateTime startUtc, DateTime endUtc) =>
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

            // Validation: startUtc < endUtc
            if (startUtc >= endUtc)
            {
                return Results.BadRequest(new { error = "Start time must be before end time." });
            }

            // Validation: range <= 365 days
            var maxRangeDays = 365;
            if ((endUtc - startUtc).TotalDays > maxRangeDays)
            {
                return Results.BadRequest(new { error = $"Time range cannot exceed {maxRangeDays} days." });
            }

            // Query Payments where PaidUtc overlaps within [startUtc, endUtc)
            // Condition: p.PaidUtc >= startUtc && p.PaidUtc < endUtc
            var payments = await db.Payments
                .AsNoTracking()
                .Where(p => p.CompanyId == tenant.CompanyId && p.PaidUtc >= startUtc && p.PaidUtc < endUtc)
                .Select(p => new { p.Amount })
                .ToListAsync();

            var totalPayments = Math.Round(payments.Sum(p => p.Amount), 2, MidpointRounding.AwayFromZero);
            var paymentCount = payments.Count;

            var response = new RevenueSummaryResponse(
                StartUtc: startUtc,
                EndUtc: endUtc,
                TotalPayments: totalPayments,
                PaymentCount: paymentCount
            );

            return Results.Ok(response);
        })
        .RequireAuthorization();

        // GET /{companySlug}/reports/occupancy - Space occupancy summary for date range (requires auth + any membership)
        group.MapGet("/reports/occupancy", async (ApplicationDbContext db, ITenantContext tenantContext, ClaimsPrincipal user, DateTime startUtc, DateTime endUtc) =>
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

            // Validation: startUtc < endUtc
            if (startUtc >= endUtc)
            {
                return Results.BadRequest(new { error = "Start time must be before end time." });
            }

            // Validation: range <= 365 days
            var maxRangeDays = 365;
            if ((endUtc - startUtc).TotalDays > maxRangeDays)
            {
                return Results.BadRequest(new { error = $"Time range cannot exceed {maxRangeDays} days." });
            }

            // Find overlapping bookings (non-cancelled only)
            // Overlap condition: startUtc < b.EndUtc && endUtc > b.StartUtc
            var overlappingBookings = await db.Bookings
                .AsNoTracking()
                .Where(b => b.CompanyId == tenant.CompanyId && !b.IsCancelled && startUtc < b.EndUtc && endUtc > b.StartUtc)
                .Select(b => new
                {
                    b.Id,
                    b.StartUtc,
                    b.EndUtc
                })
                .ToListAsync();

            // Get all space IDs for overlapping bookings in a single query
            var bookingIds = overlappingBookings.Select(b => b.Id).ToList();
            var bookingSpaces = await db.BookingSpaces
                .AsNoTracking()
                .Where(bs => bookingIds.Contains(bs.BookingId))
                .Select(bs => new
                {
                    bs.BookingId,
                    bs.SpaceId
                })
                .ToListAsync();

            // Get all relevant spaces with names
            var relevantSpaceIds = bookingSpaces.Select(bs => bs.SpaceId).Distinct().ToList();
            var spaces = await db.Spaces
                .AsNoTracking()
                .Where(s => s.CompanyId == tenant.CompanyId && relevantSpaceIds.Contains(s.Id))
                .Select(s => new
                {
                    s.Id,
                    s.Name
                })
                .ToListAsync();

            // Build lookup for space name by ID
            var spaceNameById = spaces.ToDictionary(s => s.Id, s => s.Name);

            // Aggregate per space: count bookings and sum overlapped minutes
            var spaceStats = new Dictionary<Guid, (int BookingCount, int TotalBookedMinutes)>();

            foreach (var bs in bookingSpaces)
            {
                var booking = overlappingBookings.First(b => b.Id == bs.BookingId);

                // Compute overlapped minutes
                // overlapStart = max(b.StartUtc, startUtc)
                // overlapEnd = min(b.EndUtc, endUtc)
                // minutes = (overlapEnd - overlapStart).TotalMinutes (round down)
                var overlapStart = booking.StartUtc > startUtc ? booking.StartUtc : startUtc;
                var overlapEnd = booking.EndUtc < endUtc ? booking.EndUtc : endUtc;
                var overlappedMinutes = (int)(overlapEnd - overlapStart).TotalMinutes;

                if (overlappedMinutes > 0)
                {
                    if (!spaceStats.TryGetValue(bs.SpaceId, out var stats))
                    {
                        stats = (0, 0);
                    }
                    spaceStats[bs.SpaceId] = (stats.BookingCount + 1, stats.TotalBookedMinutes + overlappedMinutes);
                }
            }

            // Build space occupancy items
            var spaceItems = spaceStats
                .Where(kv => spaceNameById.ContainsKey(kv.Key))
                .Select(kv => new SpaceOccupancyItem(
                    SpaceId: kv.Key,
                    SpaceName: spaceNameById[kv.Key],
                    BookingCount: kv.Value.BookingCount,
                    TotalBookedMinutes: kv.Value.TotalBookedMinutes
                ))
                .OrderBy(si => si.SpaceName)
                .ToList();

            // TotalBookings = distinct booking count that overlap the range
            var totalBookings = overlappingBookings.Count;

            var response = new SpaceOccupancyResponse(
                StartUtc: startUtc,
                EndUtc: endUtc,
                TotalBookings: totalBookings,
                Spaces: spaceItems
            );

            return Results.Ok(response);
        })
        .RequireAuthorization();

        // GET /{companySlug}/reports/revenue/daily - Daily revenue time-series for date range (requires auth + any membership)
        group.MapGet("/reports/revenue/daily", async (ApplicationDbContext db, ITenantContext tenantContext, ClaimsPrincipal user, DateTime startUtc, DateTime endUtc, bool? includeZeroDays = false) =>
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

            // Pro-only gate for includeZeroDays
            if (includeZeroDays == true)
            {
                var company = await db.Companies
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Id == tenant.CompanyId);

                if (company is null || !PlanFeatures.AllowsReportZeroFill(company.Plan))
                {
                    return Results.Json(new { error = "This feature requires Pro plan.", feature = "ReportZeroFill" }, statusCode: 403);
                }
            }

            // Validation: startUtc < endUtc
            if (startUtc >= endUtc)
            {
                return Results.BadRequest(new { error = "Start time must be before end time." });
            }

            // Validation: range <= 365 days
            var maxRangeDays = 365;
            if ((endUtc - startUtc).TotalDays > maxRangeDays)
            {
                return Results.BadRequest(new { error = $"Time range cannot exceed {maxRangeDays} days." });
            }

            // Query Payments where PaidUtc is within [startUtc, endUtc)
            // Group by UTC date and aggregate
            var payments = await db.Payments
                .AsNoTracking()
                .Where(p => p.CompanyId == tenant.CompanyId && p.PaidUtc >= startUtc && p.PaidUtc < endUtc)
                .Select(p => new { p.Amount, p.PaidUtc })
                .ToListAsync();

            // Group by UTC date and calculate daily totals
            var dailyData = payments
                .GroupBy(p => DateOnly.FromDateTime(p.PaidUtc))
                .ToDictionary(
                    g => g.Key,
                    g => new DailyRevenueItem(
                        DateUtc: g.Key,
                        TotalPayments: Math.Round(g.Sum(p => p.Amount), 2, MidpointRounding.AwayFromZero),
                        PaymentCount: g.Count()
                    )
                );

            IReadOnlyList<DailyRevenueItem> dailyGroups;

            if (includeZeroDays == true)
            {
                // Build full list of days in range with zero-fill
                var startDate = DateOnly.FromDateTime(startUtc);
                var endDate = DateOnly.FromDateTime(endUtc.AddDays(-1)); // endUtc is exclusive

                var allDays = new List<DailyRevenueItem>();
                for (var date = startDate; date <= endDate; date = date.AddDays(1))
                {
                    if (dailyData.TryGetValue(date, out var item))
                    {
                        allDays.Add(item);
                    }
                    else
                    {
                        allDays.Add(new DailyRevenueItem(
                            DateUtc: date,
                            TotalPayments: 0m,
                            PaymentCount: 0
                        ));
                    }
                }
                dailyGroups = allDays;
            }
            else
            {
                // Return only days with data (original behavior)
                dailyGroups = dailyData.Values.OrderBy(d => d.DateUtc).ToList();
            }

            var response = new DailyRevenueResponse(
                StartUtc: startUtc,
                EndUtc: endUtc,
                Days: dailyGroups
            );

            return Results.Ok(response);
        })
        .RequireAuthorization();

        // GET /{companySlug}/reports/occupancy/daily - Daily space occupancy time-series for date range (requires auth + any membership)
        group.MapGet("/reports/occupancy/daily", async (ApplicationDbContext db, ITenantContext tenantContext, ClaimsPrincipal user, DateTime startUtc, DateTime endUtc, bool? includeZeroDays = false, bool? onlyActive = true) =>
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

            // Pro-only gate for includeZeroDays
            if (includeZeroDays == true)
            {
                var company = await db.Companies
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Id == tenant.CompanyId);

                if (company is null || !PlanFeatures.AllowsReportZeroFill(company.Plan))
                {
                    return Results.Json(new { error = "This feature requires Pro plan.", feature = "ReportZeroFill" }, statusCode: 403);
                }
            }

            // Validation: startUtc < endUtc
            if (startUtc >= endUtc)
            {
                return Results.BadRequest(new { error = "Start time must be before end time." });
            }

            // Validation: range <= 90 days (daily occupancy can get large; keep it smaller)
            var maxRangeDays = 90;
            if ((endUtc - startUtc).TotalDays > maxRangeDays)
            {
                return Results.BadRequest(new { error = $"Time range cannot exceed {maxRangeDays} days." });
            }

            // Determine spaces to include
            var spacesQuery = db.Spaces
                .AsNoTracking()
                .Where(s => s.CompanyId == tenant.CompanyId);

            if (onlyActive != false) // default is true
            {
                spacesQuery = spacesQuery.Where(s => s.IsActive);
            }

            var spaces = await spacesQuery
                .Select(s => new
                {
                    s.Id,
                    s.Name
                })
                .ToListAsync();

            var spaceCount = spaces.Count;
            var startDate = DateOnly.FromDateTime(startUtc);
            var endDate = DateOnly.FromDateTime(endUtc.AddDays(-1)); // endUtc is exclusive
            var dayCount = endDate.DayNumber - startDate.DayNumber + 1;

            // Volume guard for includeZeroDays
            if (includeZeroDays == true && (long)spaceCount * dayCount > 50_000)
            {
                return Results.BadRequest(new { error = "Report too large. Reduce range or disable includeZeroDays." });
            }

            var spaceNameById = spaces.ToDictionary(s => s.Id, s => s.Name);

            // 1) Load overlapping bookings (non-cancelled) within range
            // Overlap predicate: startUtc < b.EndUtc && endUtc > b.StartUtc
            var overlappingBookings = await db.Bookings
                .AsNoTracking()
                .Where(b => b.CompanyId == tenant.CompanyId && !b.IsCancelled && startUtc < b.EndUtc && endUtc > b.StartUtc)
                .Select(b => new
                {
                    b.Id,
                    b.StartUtc,
                    b.EndUtc
                })
                .ToListAsync();

            // 2) Load their attached spaces (BookingSpaces) + Space names
            var bookingIds = overlappingBookings.Select(b => b.Id).ToList();
            var bookingSpaces = await db.BookingSpaces
                .AsNoTracking()
                .Where(bs => bookingIds.Contains(bs.BookingId))
                .Select(bs => new
                {
                    bs.BookingId,
                    bs.SpaceId
                })
                .ToListAsync();

            // 3) For each booking + each attached space, split across UTC days
            // Accumulate per (DateOnly, SpaceId): BookingCount and TotalBookedMinutes
            var daySpaceStats = new Dictionary<(DateOnly Date, Guid SpaceId), (int BookingCount, int TotalBookedMinutes)>();

            foreach (var bs in bookingSpaces)
            {
                var booking = overlappingBookings.First(b => b.Id == bs.BookingId);
                var spaceId = bs.SpaceId;

                if (!spaceNameById.ContainsKey(spaceId))
                    continue;

                // Compute the overlapped interval between booking and query range
                var overlapStart = booking.StartUtc > startUtc ? booking.StartUtc : startUtc;
                var overlapEnd = booking.EndUtc < endUtc ? booking.EndUtc : endUtc;

                if (overlapStart >= overlapEnd)
                    continue;

                // Split across UTC days
                var currentDay = DateOnly.FromDateTime(overlapStart);
                var endDay = DateOnly.FromDateTime(overlapEnd);

                while (currentDay <= endDay)
                {
                    var dayStartUtc = currentDay.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
                    var nextDayStartUtc = dayStartUtc.AddDays(1);

                    var dayIntervalStart = overlapStart > dayStartUtc ? overlapStart : dayStartUtc;
                    var dayIntervalEnd = overlapEnd < nextDayStartUtc ? overlapEnd : nextDayStartUtc;

                    if (dayIntervalStart < dayIntervalEnd)
                    {
                        var minutes = (int)(dayIntervalEnd - dayIntervalStart).TotalMinutes;

                        var key = (currentDay, spaceId);
                        if (!daySpaceStats.TryGetValue(key, out var stats))
                        {
                            stats = (0, 0);
                        }
                        // Count booking once per day per space
                        daySpaceStats[key] = (stats.BookingCount + 1, stats.TotalBookedMinutes + minutes);
                    }

                    currentDay = currentDay.AddDays(1);
                }
            }

            IReadOnlyList<DailySpaceOccupancyItem> items;

            if (includeZeroDays == true)
            {
                // Build full matrix of day + space combinations with zero-fill
                var allItems = new List<DailySpaceOccupancyItem>();

                for (var date = startDate; date <= endDate; date = date.AddDays(1))
                {
                    foreach (var space in spaces)
                    {
                        var key = (date, space.Id);
                        if (daySpaceStats.TryGetValue(key, out var stats))
                        {
                            allItems.Add(new DailySpaceOccupancyItem(
                                DateUtc: date,
                                SpaceId: space.Id,
                                SpaceName: space.Name,
                                BookingCount: stats.BookingCount,
                                TotalBookedMinutes: stats.TotalBookedMinutes
                            ));
                        }
                        else
                        {
                            allItems.Add(new DailySpaceOccupancyItem(
                                DateUtc: date,
                                SpaceId: space.Id,
                                SpaceName: space.Name,
                                BookingCount: 0,
                                TotalBookedMinutes: 0
                            ));
                        }
                    }
                }

                items = allItems
                    .OrderBy(i => i.DateUtc)
                    .ThenBy(i => i.SpaceName)
                    .ToList();
            }
            else
            {
                // Return only days/spaces that have occupancy (original behavior)
                items = daySpaceStats
                    .Where(kv => spaceNameById.ContainsKey(kv.Key.SpaceId))
                    .Select(kv => new DailySpaceOccupancyItem(
                        DateUtc: kv.Key.Date,
                        SpaceId: kv.Key.SpaceId,
                        SpaceName: spaceNameById[kv.Key.SpaceId],
                        BookingCount: kv.Value.BookingCount,
                        TotalBookedMinutes: kv.Value.TotalBookedMinutes
                    ))
                    .OrderBy(i => i.DateUtc)
                    .ThenBy(i => i.SpaceName)
                    .ToList();
            }

            var response = new DailySpaceOccupancyResponse(
                StartUtc: startUtc,
                EndUtc: endUtc,
                Items: items
            );

            return Results.Ok(response);
        })
        .RequireAuthorization();

        return group;
    }
}
