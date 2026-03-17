using App.BLL.Mappers;
using App.DAL.EF;
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
            .Include(venue => venue.Bookings)
            .OrderBy(venue => venue.Name);

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
}
