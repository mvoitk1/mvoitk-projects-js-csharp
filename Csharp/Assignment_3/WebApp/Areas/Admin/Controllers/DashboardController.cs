using App.DAL.EF;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace WebApp.Areas.Admin.Controllers;

public class DashboardController(AppDbContext db) : AdminBaseController
{
    public async Task<IActionResult> Index()
    {
        ViewData["ProductCount"] = await db.Products.CountAsync();
        ViewData["OrderCount"] = await db.Orders.CountAsync();
        ViewData["LowStockCount"] = await db.ProductVariants.CountAsync(v => v.StockQty < 5 && v.IsActive);
        ViewData["RecentOrderCount"] = await db.Orders
            .CountAsync(o => o.CreatedAt >= DateTime.UtcNow.AddDays(-1));
        return View();
    }
}
