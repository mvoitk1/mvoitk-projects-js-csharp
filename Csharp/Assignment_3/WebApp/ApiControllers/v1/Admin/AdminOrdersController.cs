using App.BLL.Services;
using App.DTO.v1.Admin;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.ApiControllers.v1.Admin;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/admin/orders")]
[ApiController]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")]
public class AdminOrdersController(IAdminOrderService orderService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<AdminOrderDto>>> GetAll([FromQuery] string? status)
        => Ok(await orderService.GetAllAsync(status));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AdminOrderDto>> GetById(Guid id)
    {
        var order = await orderService.GetByIdAsync(id);
        return order == null ? NotFound() : Ok(order);
    }

    [HttpPut("{id:guid}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] AdminOrderStatusDto dto)
    {
        return await orderService.UpdateStatusAsync(id, dto.Status) ? NoContent() : NotFound();
    }
}
