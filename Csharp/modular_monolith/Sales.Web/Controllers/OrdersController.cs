using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Modules.SharedKernel;
using Modules.SharedKernel.Mapping;
using Sales.Application.Contracts;
using Sales.Web.Dtos.v1.Orders;

namespace Sales.Web.Controllers;

[Authorize]
public class OrdersController(IOrderService orderService) : Controller
{
    public async Task<IActionResult> Index()
    {
        var orders = await orderService.GetUserOrdersAsync(User.UserId());
        return View(orders.Cast<object>().MapList<OrderListItemDto>());
    }

    public async Task<IActionResult> Detail(Guid id)
    {
        var order = await orderService.GetUserOrderByIdAsync(User.UserId(), id);
        if (order == null) return NotFound();
        return View(order.MapTo<OrderDto>());
    }
}
