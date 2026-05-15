using App.BLL.Contracts;
using App.DTO.v1.Admin;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.ApiControllers.v1.Admin;

/// <summary>Back-office stock quantity management. Requires <c>Admin</c> role.</summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/admin/stock")]
[ApiController]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")]
public class AdminStockController(IAdminCatalogueService catalogueService) : ControllerBase
{
    /// <summary>Set the stock quantity for a product variant.</summary>
    /// <param name="variantId">Product variant ID.</param>
    [HttpPut("{variantId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStock(Guid variantId, [FromBody] AdminStockUpdateDto dto)
        => await catalogueService.UpdateStockAsync(variantId, dto.StockQty) ? NoContent() : NotFound();
}
