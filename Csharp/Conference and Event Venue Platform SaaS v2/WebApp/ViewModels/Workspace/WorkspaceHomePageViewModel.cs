using App.DTO.v1.Venues.Public;

namespace WebApp.ViewModels.Workspace;

public class WorkspaceHomePageViewModel
{
    public WorkspaceLayoutViewModel Layout { get; init; } = default!;
    public IReadOnlyList<UserVenueAccessRequestSummaryDto> Requests { get; init; } = [];
    public bool HasRequests => Requests.Count > 0;
}
