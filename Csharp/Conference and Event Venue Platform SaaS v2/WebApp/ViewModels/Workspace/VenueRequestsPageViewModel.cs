using App.DTO.v1.Venues.Admin;

namespace WebApp.ViewModels.Workspace;

public class VenueRequestsPageViewModel
{
    public WorkspaceLayoutViewModel Layout { get; init; } = new();
    public IReadOnlyList<VenueAccessRequestListItemDto> Requests { get; init; } = [];
    public VenueAccessRequestDetailDto? SelectedRequest { get; init; }
    public ReviewVenueAccessRequestViewModel ReviewForm { get; init; } = new();
}
