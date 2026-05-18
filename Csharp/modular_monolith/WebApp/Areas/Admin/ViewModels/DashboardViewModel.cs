namespace WebApp.Areas.Admin.ViewModels;

public class DashboardViewModel
{
    public int ProductCount { get; init; }
    public int OrderCount { get; init; }
    public int LowStockCount { get; init; }
    public int RecentOrderCount { get; init; }
}
