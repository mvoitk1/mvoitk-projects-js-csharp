using Catalog.Application.Contracts;
using Microsoft.AspNetCore.Mvc;
using Catalog.Web.Areas.Admin.ViewModels;

namespace Catalog.Web.Areas.Admin.Controllers;

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
