using App.DTO.v1.Venues.Employee;

namespace WebApp.ViewModels.Workspace;

public class EmployeeCoordinationPageViewModel
{
    public WorkspaceLayoutViewModel Layout { get; init; } = new();
    public IReadOnlyList<EmployeeBookingSummaryDto> AvailableBookings { get; init; } = [];
    public EmployeeBookingCoordinationDto? Coordination { get; init; }
}
