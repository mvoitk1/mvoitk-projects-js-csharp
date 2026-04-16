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
        var ok = await orderService.UpdateStatusAsync(id, status);
        TempData[ok ? "Success" : "Error"] = ok
            ? $"Status updated to {status}."
            : "Failed to update status — invalid status value or order not found.";
        return RedirectToAction(nameof(Detail), "Orders", new { area = "Admin", id });
    }
}
