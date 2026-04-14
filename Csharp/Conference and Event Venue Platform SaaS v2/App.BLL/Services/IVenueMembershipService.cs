using App.DTO.v1.Venues.Membership;

namespace App.BLL.Services;

public interface IVenueMembershipService
{
    Task<IReadOnlyList<ActiveVenueOptionDto>> GetVenueOptionsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<ActiveVenueSelectionResultDto?> GetActiveVenueAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<ActiveVenueSelectionResultDto> SetActiveVenueAsync(Guid userId, Guid venueId, CancellationToken cancellationToken = default);
    Task<VenueMembershipSummaryDto> AssignVenueMembershipAsync(AssignVenueMembershipDto dto, CancellationToken cancellationToken = default);
    Task<VenueMembershipSummaryDto> UpdateMembershipStatusAsync(
        Guid membershipId,
        UpdateVenueMembershipStatusDto dto,
        CancellationToken cancellationToken = default);
}
