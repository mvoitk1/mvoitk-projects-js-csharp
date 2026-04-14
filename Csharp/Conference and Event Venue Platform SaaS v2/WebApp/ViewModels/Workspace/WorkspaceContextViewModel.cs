using App.DTO.v1.Venues.Membership;

namespace WebApp.ViewModels.Workspace;

public class WorkspaceContextViewModel
{
    public ActiveVenueSelectionResultDto? ActiveVenue { get; init; }
    public IReadOnlyList<ActiveVenueOptionDto> VenueOptions { get; init; } = [];
    public bool HasVenueAccess => VenueOptions.Count > 0;
}
