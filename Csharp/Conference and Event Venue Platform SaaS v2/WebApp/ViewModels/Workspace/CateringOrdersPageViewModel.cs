using App.DTO.v1.Venues.Employee;

namespace WebApp.ViewModels.Workspace;

public class CateringOrdersPageViewModel
{
    public WorkspaceLayoutViewModel Layout { get; init; } = new();
    public IReadOnlyList<CateringOrderSummaryDto> Orders { get; init; } = [];
    public CateringOrderSummaryDto? SelectedOrder { get; init; }
    public CateringOrderEditViewModel EditForm { get; init; } = new();
}
