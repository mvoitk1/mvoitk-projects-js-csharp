using Sales.Web.Dtos.v1.Admin;
using Sales.Application.Contracts;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Sales.Web.ApiControllers.v1.Admin;

/// <summary>Back-office order management. Requires <c>Admin</c> role.</summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/admin/orders")]
[ApiController]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")]
public class AdminOrdersController(IAdminOrderService orderService) : ControllerBase
{
    /// <summary>List all orders, optionally filtered by status.</summary>
    /// <param name="status">Order status filter (e.g. <c>Pending</c>, <c>Shipped</c>, <c>Delivered</c>).</param>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<AdminOrderDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<AdminOrderDto>>> GetAll([FromQuery] string? status)
        => Ok(await orderService.GetAllAsync(status));

    /// <summary>Get a single order by ID.</summary>
    /// <param name="id">Order ID.</param>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(AdminOrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminOrderDto>> GetById(Guid id)
    {
        var order = await orderService.GetByIdAsync(id);
        return order == null ? NotFound() : Ok(order);
    }

    /// <summary>Update the fulfillment status of an order.</summary>
    /// <param name="id">Order ID.</param>
    /// <param name="dto">New order status.</param>
    [HttpPut("{id:guid}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] AdminOrderStatusDto dto)
        => await orderService.UpdateStatusAsync(id, dto.Status) ? NoContent() : NotFound();
}
