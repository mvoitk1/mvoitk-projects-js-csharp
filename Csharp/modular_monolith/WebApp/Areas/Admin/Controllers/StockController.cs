using Catalog.Application.Contracts;
using App.DTO.Mappers;
using App.DTO.v1.Admin;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.Areas.Admin.Controllers;

public class StockController(IAdminCatalogueService catalogueService) : AdminBaseController
{
    public async Task<IActionResult> Index()
    {
        var items = await catalogueService.GetAllStockItemsAsync();
        return View(items.MapList<AdminStockItemDto>());
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
