using App.BLL.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace WebApp.Areas.Admin.Controllers;

public class OrdersController(IAdminOrderService orderService) : AdminBaseController
{
    public async Task<IActionResult> Index(string? status)
    {
        ViewData["StatusFilter"] = status;
        ViewData["Statuses"] = new SelectList(new[]
        {
            "", "Pending", "Confirmed", "Processing", "Shipped", "Delivered", "Cancelled"
        });
        return View(await orderService.GetAllAsync(status));
    }

    public async Task<IActionResult> Detail(Guid id)
    {
        var order = await orderService.GetByIdAsync(id);
        if (order == null) return NotFound();
        ViewData["Statuses"] = new SelectList(new[]
        {
            "Pending", "Confirmed", "Processing", "Shipped", "Delivered", "Cancelled"
        }, order.Status);
        return View(order);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(Guid id, string status)
    {
        await orderService.UpdateStatusAsync(id, status);
        return RedirectToAction(nameof(Detail), new { area = "Admin", id });
    }
}
