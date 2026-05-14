using App.BLL.Services;
using Microsoft.AspNetCore.Mvc;
using WebApp.Areas.Admin.ViewModels;

namespace WebApp.Areas.Admin.Controllers;

public class DashboardController(IAdminCatalogueService catalogueService) : AdminBaseController
{
    public async Task<IActionResult> Index()
    {
        var stats = await catalogueService.GetDashboardStatsAsync();
        return View(new DashboardViewModel
        {
            ProductCount = stats.ProductCount,
            OrderCount = stats.OrderCount,
            LowStockCount = stats.LowStockCount,
            RecentOrderCount = stats.RecentOrderCount
        });
    }
}
