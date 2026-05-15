using App.BLL.Contracts;
using App.DTO.Mappers;
using App.DTO.v1.Orders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.Helpers;

namespace WebApp.Controllers;

[Authorize]
public class OrdersController(IOrderService orderService) : Controller
{
    public async Task<IActionResult> Index()
    {
        var orders = await orderService.GetUserOrdersAsync(User.UserId());
        return View(orders.MapList<OrderListItemDto>());
    }

    public async Task<IActionResult> Detail(Guid id)
    {
        var order = await orderService.GetUserOrderByIdAsync(User.UserId(), id);
        if (order == null) return NotFound();
        return View(order.MapTo<OrderDto>());
    }
}
