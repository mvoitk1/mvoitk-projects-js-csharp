using App.BLL.Services;
using App.DAL.EF;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace WebApp.Areas.Admin.Controllers;

public class StockController(IAdminCatalogueService catalogueService, AppDbContext db) : AdminBaseController
{
    public async Task<IActionResult> Index()
    {
        var variants = await db.ProductVariants
            .Include(v => v.Product)
            .Include(v => v.Color)
            .Include(v => v.Size)
            .Where(v => v.IsActive)
            .OrderBy(v => v.StockQty)
            .ToListAsync();

        return View(variants);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStock(Guid variantId, int stockQty)
    {
        await catalogueService.UpdateStockAsync(variantId, stockQty);
        return RedirectToAction(nameof(Index));
    }
}
