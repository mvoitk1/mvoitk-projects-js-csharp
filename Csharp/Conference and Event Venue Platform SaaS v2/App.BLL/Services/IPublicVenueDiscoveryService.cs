using App.DTO.v1.Venues.Public;

namespace App.BLL.Services;

public interface IPublicVenueDiscoveryService
{
    Task<PublicLandingPageDto> GetLandingPageAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PublicVenueSummaryDto>> GetBrowseVenuesAsync(CancellationToken cancellationToken = default);
    Task<PublicVenueDetailDto?> GetVenueAsync(string slug, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UserVenueAccessRequestSummaryDto>> GetUserVenueAccessRequestsAsync(
        Guid requestorUserId,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UserBookingRequestSummaryDto>> GetUserBookingRequestsAsync(
        Guid requestorUserId,
        CancellationToken cancellationToken = default);
    Task<UserBookingRequestDetailDto?> GetUserBookingRequestAsync(
        Guid requestorUserId,
        Guid bookingId,
        CancellationToken cancellationToken = default);
    Task<PublicBookingRequestSubmissionResultDto> SubmitBookingRequestAsync(
        Guid requestorUserId,
        string venueSlug,
        SubmitPublicBookingRequestDto dto,
        CancellationToken cancellationToken = default);
    Task<VenueAccessRequestSubmissionResultDto> SubmitVenueAccessRequestAsync(
        Guid requestorUserId,
        SubmitVenueAccessRequestDto dto,
        CancellationToken cancellationToken = default);
}
