using App.BLL.Contracts;
using App.DTO.Mappers;
using App.DTO.v1.Orders;
using Asp.Versioning;
using BllOrders = App.BLL.DTO.Orders;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.Helpers;

namespace WebApp.ApiControllers.v1;

/// <summary>Order history and checkout for the authenticated customer.</summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiController]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class OrdersController(IOrderService orderService) : ControllerBase
{
    /// <summary>List all orders placed by the current user.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<OrderListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<OrderListItemDto>>> GetOrders()
    {
        var userId = User.UserId();
        return Ok(await orderService.GetUserOrdersAsync(userId));
    }

    /// <summary>Get full details of a single order belonging to the current user.</summary>
    /// <param name="id">Order ID.</param>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderDto>> GetOrder(Guid id)
    {
        var userId = User.UserId();
        var order = await orderService.GetUserOrderByIdAsync(userId, id);
        if (order == null) return NotFound();
        return Ok(order);
    }

    /// <summary>Place an order from the current cart.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<OrderDto>> PlaceOrder([FromBody] CreateOrderDto dto)
    {
        try
        {
            var userId = User.UserId();
            var order = await orderService.PlaceOrderAsync(userId, dto.MapTo<BllOrders.CreateOrderDto>());
            return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, order);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
