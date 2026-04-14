using App.BLL.Services;
using App.DAL.EF;
using App.DTO.v1.Admin;
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

        var items = variants.Select(v => new AdminStockItemDto
        {
            VariantId = v.Id,
            ProductName = v.Product?.Name.Translate() ?? string.Empty,
            Sku = v.Sku,
            ColorName = v.Color?.Name.Translate() ?? string.Empty,
            SizeCode = v.Size?.SizeCode ?? string.Empty,
            StockQty = v.StockQty
        });

        return View(items);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStock(Guid variantId, int stockQty)
    {
        await catalogueService.UpdateStockAsync(variantId, stockQty);
        return RedirectToAction(nameof(Index));
    }
}
