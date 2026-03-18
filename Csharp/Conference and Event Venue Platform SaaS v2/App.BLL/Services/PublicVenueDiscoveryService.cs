using App.BLL.Mappers;
using App.DAL.EF;
using App.Domain.ValueObjects;
using App.Domain.Venues;
using App.DTO.v1.Venues.Public;
using Microsoft.EntityFrameworkCore;

namespace App.BLL.Services;

public class PublicVenueDiscoveryService(AppDbContext context) : IPublicVenueDiscoveryService
{
    public async Task<PublicLandingPageDto> GetLandingPageAsync(CancellationToken cancellationToken = default)
    {
        var venues = await QueryActiveVenues()
            .Take(3)
            .ToListAsync(cancellationToken);

        return new PublicLandingPageDto
        {
            ActiveVenueCount = await context.Venues.CountAsync(venue => venue.Status == VenueLifecycleStatus.Active, cancellationToken),
            CityCount = await context.Venues
                .Where(venue => venue.Status == VenueLifecycleStatus.Active)
                .Select(venue => venue.City)
                .Distinct()
                .CountAsync(cancellationToken),
            UpcomingConfirmedBookingCount = await context.Bookings.CountAsync(
                booking => booking.Status == BookingStatus.Confirmed && booking.Schedule.StartsAt >= DateTime.UtcNow,
                cancellationToken),
            FeaturedVenues = venues.Select(venue => venue.ToPublicSummaryDto()).ToList()
        };
    }

    public async Task<IReadOnlyList<PublicVenueSummaryDto>> GetBrowseVenuesAsync(CancellationToken cancellationToken = default)
    {
        var venues = await QueryActiveVenues().ToListAsync(cancellationToken);
        return venues.Select(venue => venue.ToPublicSummaryDto()).ToList();
    }

    public async Task<PublicVenueDetailDto?> GetVenueAsync(string slug, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            return null;
        }

        var venue = await QueryActiveVenues()
            .SingleOrDefaultAsync(item => item.Slug == slug.Trim(), cancellationToken);

        return venue?.ToPublicDetailDto();
    }

    public async Task<IReadOnlyList<UserVenueAccessRequestSummaryDto>> GetUserVenueAccessRequestsAsync(
        Guid requestorUserId,
        CancellationToken cancellationToken = default)
    {
        var requests = await context.VenueAccessRequests
            .AsNoTracking()
            .Where(item => item.RequestorUserId == requestorUserId)
            .Select(item => new UserVenueAccessRequestSummaryDto
            {
                RequestId = item.Id,
                CompanyName = item.CompanyName,
                VenueName = item.VenueName,
                Status = item.Status.ToString(),
                SubmittedAt = item.SubmittedAt,
                ReviewedAt = item.ReviewedAt,
                ApprovedAccessLevel = item.ApprovedAccessLevel != null ? item.ApprovedAccessLevel.ToString() : null,
                HasAssignedMembership = item.VenueId.HasValue &&
                                        context.VenueMemberships.Any(membership =>
                                            membership.UserId == item.RequestorUserId &&
                                            membership.VenueId == item.VenueId.Value)
            })
            .OrderByDescending(item => item.SubmittedAt)
            .ToListAsync(cancellationToken);

        return requests;
    }

    public async Task<IReadOnlyList<UserBookingRequestSummaryDto>> GetUserBookingRequestsAsync(
        Guid requestorUserId,
        CancellationToken cancellationToken = default)
    {
        var bookings = await context.Bookings
            .AsNoTracking()
            .Where(item => item.CreatedByUserId == requestorUserId)
            .Include(item => item.Venue)
            .Include(item => item.Space)
            .ToListAsync(cancellationToken);

        return bookings
            .OrderBy(item => item.Status == BookingStatus.PendingApproval ? 0 : 1)
            .ThenBy(item => item.Schedule.StartsAt)
            .Select(item => item.ToUserBookingRequestSummaryDto())
            .ToList();
    }

    public async Task<UserBookingRequestDetailDto?> GetUserBookingRequestAsync(
        Guid requestorUserId,
        Guid bookingId,
        CancellationToken cancellationToken = default)
    {
        var booking = await context.Bookings
            .AsNoTracking()
            .Where(item => item.Id == bookingId && item.CreatedByUserId == requestorUserId)
            .Include(item => item.Venue)
            .Include(item => item.Space)
            .Include(item => item.CateringOrders)
            .Include(item => item.EquipmentAllocations)
            .SingleOrDefaultAsync(cancellationToken);

        return booking?.ToUserBookingRequestDetailDto();
    }

    public async Task<PublicBookingRequestSubmissionResultDto> SubmitBookingRequestAsync(
        Guid requestorUserId,
        string venueSlug,
        SubmitPublicBookingRequestDto dto,
        CancellationToken cancellationToken = default)
    {
        ValidateBookingRequest(dto);

        var venue = await context.Venues
            .Include(item => item.Spaces)
                .ThenInclude(space => space.Layouts)
            .SingleOrDefaultAsync(
                item => item.Status == VenueLifecycleStatus.Active &&
                        item.Slug == venueSlug.Trim(),
                cancellationToken);

        if (venue == null)
        {
            throw new KeyNotFoundException("Venue was not found.");
        }

        var space = venue.Spaces.SingleOrDefault(item => item.Id == dto.SpaceId);
        if (space == null)
        {
            throw new KeyNotFoundException("Space was not found.");
        }

        if (space.Status != SpaceStatus.Active)
        {
            throw new InvalidOperationException("Only active spaces can accept booking requests.");
        }

        var layout = dto.LayoutId.HasValue
            ? space.Layouts.SingleOrDefault(item => item.Id == dto.LayoutId.Value)
            : space.Layouts.FirstOrDefault(item => item.IsDefault);

        if (dto.LayoutId.HasValue && layout == null)
        {
            throw new InvalidOperationException("The selected layout does not belong to this space.");
        }

        if (dto.ExpectedAttendees > space.CapacityProfile.Maximum)
        {
            throw new InvalidOperationException("The attendee count exceeds the selected space capacity.");
        }

        if (layout != null && dto.ExpectedAttendees > layout.Capacity)
        {
            throw new InvalidOperationException("The attendee count exceeds the selected layout capacity.");
        }

        var schedule = new ScheduleWindow(dto.StartsAt, dto.EndsAt);
        if (schedule.DurationMinutes < space.MinimumBookingDurationMinutes)
        {
            throw new InvalidOperationException("The requested time window is shorter than the minimum booking duration.");
        }

        var hasConfirmedConflict = await context.Bookings
            .AsNoTracking()
            .Where(item => item.SpaceId == space.Id && item.Status == BookingStatus.Confirmed)
            .AnyAsync(
                item => item.Schedule.StartsAt < dto.EndsAt &&
                        dto.StartsAt < item.Schedule.EndsAt,
                cancellationToken);

        if (hasConfirmedConflict)
        {
            throw new InvalidOperationException("The selected space is not available for the requested time.");
        }

        var booking = new Booking
        {
            VenueId = venue.Id,
            SpaceId = space.Id,
            CreatedByUserId = requestorUserId,
            Title = dto.EventTitle.Trim(),
            ClientName = dto.ClientName.Trim(),
            Status = BookingStatus.PendingApproval,
            Schedule = schedule,
            ExpectedAttendees = dto.ExpectedAttendees,
            SpaceCharge = CalculateSpaceCharge(space.HourlyRate, schedule.DurationMinutes),
            CoordinationNotes = BuildCoordinationNotes(layout, dto)
        };

        context.Bookings.Add(booking);

        if (!string.IsNullOrWhiteSpace(dto.CateringNotes))
        {
            context.CateringOrders.Add(new CateringOrder
            {
                Booking = booking,
                ProviderName = "Pending venue confirmation",
                Status = CateringOrderStatus.Draft,
                LockedAt = dto.StartsAt.AddHours(-72),
                GuestCount = dto.ExpectedAttendees,
                TotalPrice = Money.Zero(space.HourlyRate.Currency),
                Notes = dto.CateringNotes.Trim()
            });
        }

        await context.SaveChangesAsync(cancellationToken);

        return new PublicBookingRequestSubmissionResultDto
        {
            BookingId = booking.Id,
            Status = booking.Status.ToString(),
            VenueId = venue.Id,
            SpaceId = space.Id
        };
    }

    public async Task<VenueAccessRequestSubmissionResultDto> SubmitVenueAccessRequestAsync(
        Guid requestorUserId,
        SubmitVenueAccessRequestDto dto,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(dto);

        var request = new VenueAccessRequest
        {
            RequestorUserId = requestorUserId,
            CompanyName = dto.CompanyName.Trim(),
            VenueName = dto.VenueName.Trim(),
            ContactName = dto.ContactName.Trim(),
            ContactEmail = dto.ContactEmail.Trim(),
            ContactPhone = string.IsNullOrWhiteSpace(dto.ContactPhone) ? null : dto.ContactPhone.Trim(),
            City = dto.City.Trim(),
            Country = dto.Country.Trim(),
            AddressLine1 = dto.AddressLine1.Trim(),
            EstimatedMonthlyBookings = dto.EstimatedMonthlyBookings,
            Notes = string.IsNullOrWhiteSpace(dto.Notes) ? null : dto.Notes.Trim(),
            Status = VenueAccessRequestStatus.PendingReview,
            SubmittedAt = DateTime.UtcNow
        };

        context.VenueAccessRequests.Add(request);
        await context.SaveChangesAsync(cancellationToken);

        return new VenueAccessRequestSubmissionResultDto
        {
            RequestId = request.Id,
            Status = request.Status.ToString(),
            SubmittedAt = request.SubmittedAt
        };
    }

    private IQueryable<Venue> QueryActiveVenues() =>
        context.Venues
            .AsNoTracking()
            .Where(venue => venue.Status == VenueLifecycleStatus.Active)
            .Include(venue => venue.Spaces)
                .ThenInclude(space => space.Layouts)
            .Include(venue => venue.Bookings)
            .OrderBy(venue => venue.Name);

    private static Money CalculateSpaceCharge(Money hourlyRate, int durationMinutes)
    {
        var hours = durationMinutes / 60m;
        return new Money(decimal.Round(hourlyRate.Amount * hours, 2), hourlyRate.Currency);
    }

    private static string? BuildCoordinationNotes(SpaceLayout? layout, SubmitPublicBookingRequestDto dto)
    {
        var lines = new List<string>();

        if (layout != null)
        {
            lines.Add($"Requested layout: {layout.Name} ({layout.LayoutType})");
        }

        if (!string.IsNullOrWhiteSpace(dto.SetupRequirements))
        {
            lines.Add($"Setup requirements: {dto.SetupRequirements.Trim()}");
        }

        if (!string.IsNullOrWhiteSpace(dto.AdditionalRequirements))
        {
            lines.Add($"Additional requirements: {dto.AdditionalRequirements.Trim()}");
        }

        return lines.Count == 0 ? null : string.Join(Environment.NewLine, lines);
    }

    private static void ValidateRequest(SubmitVenueAccessRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.CompanyName) ||
            string.IsNullOrWhiteSpace(dto.VenueName) ||
            string.IsNullOrWhiteSpace(dto.ContactName) ||
            string.IsNullOrWhiteSpace(dto.ContactEmail) ||
            string.IsNullOrWhiteSpace(dto.City) ||
            string.IsNullOrWhiteSpace(dto.Country) ||
            string.IsNullOrWhiteSpace(dto.AddressLine1))
        {
            throw new ArgumentException("All required request fields must be provided.");
        }

        if (dto.EstimatedMonthlyBookings <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(dto.EstimatedMonthlyBookings));
        }
    }

    private static void ValidateBookingRequest(SubmitPublicBookingRequestDto dto)
    {
        if (dto.SpaceId == Guid.Empty ||
            string.IsNullOrWhiteSpace(dto.EventTitle) ||
            string.IsNullOrWhiteSpace(dto.ClientName))
        {
            throw new ArgumentException("All required booking request fields must be provided.");
        }

        if (dto.ExpectedAttendees <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(dto.ExpectedAttendees));
        }

        if (dto.StartsAt < DateTime.UtcNow.AddMinutes(-1))
        {
            throw new InvalidOperationException("Booking requests must be scheduled in the future.");
        }
    }
}
