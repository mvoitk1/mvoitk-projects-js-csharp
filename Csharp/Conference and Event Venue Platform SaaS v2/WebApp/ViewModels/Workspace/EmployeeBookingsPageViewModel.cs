using App.DTO.v1.Venues.Employee;

namespace WebApp.ViewModels.Workspace;

public class EmployeeBookingsPageViewModel
{
    public WorkspaceLayoutViewModel Layout { get; init; } = new();
    public IReadOnlyList<EmployeeBookingSummaryDto> Bookings { get; init; } = [];
    public BookingCalendarSectionViewModel BookingCalendar { get; init; } = new();
}
