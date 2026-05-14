using App.BLL.Services;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.Areas.Admin.Controllers;

public class StockController(IAdminCatalogueService catalogueService) : AdminBaseController
{
    public async Task<IActionResult> Index()
    {
        return View(await catalogueService.GetAllStockItemsAsync());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStock(Guid variantId, int stockQty)
    {
        try
        {
            var found = await catalogueService.UpdateStockAsync(variantId, stockQty);
            TempData[found ? "StockSuccess" : "StockError"] = found
                ? $"Stock updated to {stockQty}."
                : "Variant not found.";
        }
        catch (Exception ex)
        {
            TempData["StockError"] = ex.Message;
        }
        return RedirectToAction(nameof(Index));
    }
}
