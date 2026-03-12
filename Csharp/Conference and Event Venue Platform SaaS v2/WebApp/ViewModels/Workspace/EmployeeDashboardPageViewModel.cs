using App.DTO.v1.Venues.Employee;

namespace WebApp.ViewModels.Workspace;

public class EmployeeDashboardPageViewModel
{
    public WorkspaceLayoutViewModel Layout { get; init; } = new();
    public EmployeeWorkspaceDashboardDto Dashboard { get; init; } = new();
}
