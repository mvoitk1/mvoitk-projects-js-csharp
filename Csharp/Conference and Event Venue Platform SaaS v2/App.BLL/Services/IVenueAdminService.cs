using App.DTO.v1.Venues.Admin;

namespace App.BLL.Services;

public interface IVenueAdminService
{
    Task<VenueAdminDashboardDto> GetDashboardAsync(Guid userId, Guid venueId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<VenueAccessRequestListItemDto>> GetVenueAccessRequestsAsync(CancellationToken cancellationToken = default);
    Task<VenueAccessRequestDetailDto> GetVenueAccessRequestAsync(Guid requestId, CancellationToken cancellationToken = default);
    Task<VenueAccessRequestDetailDto> ReviewVenueAccessRequestAsync(
        Guid reviewedByUserId,
        ReviewVenueAccessRequestDto dto,
        CancellationToken cancellationToken = default);
    Task ArchiveRejectedVenueAsync(
        Guid reviewedByUserId,
        Guid requestId,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SpaceConfigurationDto>> GetSpaceConfigurationsAsync(
        Guid userId,
        Guid venueId,
        CancellationToken cancellationToken = default);
    Task<SpaceConfigurationDto> SaveSpaceConfigurationAsync(
        Guid userId,
        Guid venueId,
        UpsertSpaceConfigurationDto dto,
        CancellationToken cancellationToken = default);
}
