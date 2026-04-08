using App.DTO.v1.Venues.Admin;

namespace WebApp.ViewModels.Workspace;

public class AdminDashboardPageViewModel
{
    public WorkspaceLayoutViewModel Layout { get; init; } = new();
    public VenueAdminDashboardDto Dashboard { get; init; } = new();
    public BookingCalendarSectionViewModel BookingCalendar { get; init; } = new();
}
