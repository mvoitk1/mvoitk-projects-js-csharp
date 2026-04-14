using App.DTO.v1.Venues.Public;

namespace WebApp.ViewModels.Workspace;

public class WorkspaceBookingRequestDetailsPageViewModel
{
    public WorkspaceLayoutViewModel Layout { get; init; } = default!;
    public UserBookingRequestDetailDto Booking { get; init; } = default!;
}
