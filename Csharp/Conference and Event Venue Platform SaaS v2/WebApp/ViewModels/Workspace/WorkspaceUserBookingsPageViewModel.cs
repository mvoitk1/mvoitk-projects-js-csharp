namespace WebApp.ViewModels.Workspace;

public class WorkspaceUserBookingsPageViewModel
{
    public WorkspaceLayoutViewModel Layout { get; init; } = new();
    public BookingCalendarSectionViewModel BookingCalendar { get; init; } = new();
}
