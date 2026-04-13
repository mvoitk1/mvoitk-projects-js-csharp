using App.BLL.Services;
using App.DTO.v1.Admin;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.ApiControllers.v1.Admin;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/admin/stock")]
[ApiController]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")]
public class AdminStockController(IAdminCatalogueService catalogueService) : ControllerBase
{
    [HttpPut("{variantId:guid}")]
    public async Task<IActionResult> UpdateStock(Guid variantId, [FromBody] AdminStockUpdateDto dto)
    {
        return await catalogueService.UpdateStockAsync(variantId, dto.StockQty) ? NoContent() : NotFound();
    }
}
