using App.BLL.Contracts;
using App.DTO.Mappers;
using App.DTO.v1.Admin;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using WebApp.Areas.Admin.ViewModels;

namespace WebApp.Areas.Admin.Controllers;

public class OrdersController(IAdminOrderService orderService) : AdminBaseController
{
    private static readonly string[] StatusValues =
        ["", "Pending", "Confirmed", "Processing", "Shipped", "Delivered", "Cancelled"];

    private static readonly string[] EditableStatuses =
        ["Pending", "Confirmed", "Processing", "Shipped", "Delivered", "Cancelled"];

    public async Task<IActionResult> Index(string? status)
    {
        return View(new OrderIndexViewModel
        {
            Orders = (await orderService.GetAllAsync(status)).Cast<object>().MapList<AdminOrderDto>(),
            Statuses = new SelectList(StatusValues),
            StatusFilter = status
        });
    }

    public async Task<IActionResult> Detail(Guid id)
    {
        var order = await orderService.GetByIdAsync(id);
        if (order == null) return NotFound();

        return View(new OrderDetailViewModel
        {
            Order = order.MapTo<AdminOrderDto>(),
            Statuses = new SelectList(EditableStatuses, order.Status)
        });
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
