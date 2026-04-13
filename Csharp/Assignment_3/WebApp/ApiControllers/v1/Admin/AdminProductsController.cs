using App.BLL.Services;
using App.DTO.v1.Admin;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.ApiControllers.v1.Admin;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/admin/products")]
[ApiController]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")]
public class AdminProductsController(IAdminProductService productService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<AdminProductDto>>> GetAll()
        => Ok(await productService.GetAllAsync());

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AdminProductDto>> GetById(Guid id)
    {
        var product = await productService.GetByIdAsync(id);
        return product == null ? NotFound() : Ok(product);
    }

    [HttpPost]
    public async Task<ActionResult<AdminProductDto>> Create([FromBody] AdminProductWriteDto dto)
    {
        var created = await productService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<AdminProductDto>> Update(Guid id, [FromBody] AdminProductWriteDto dto)
    {
        var updated = await productService.UpdateAsync(id, dto);
        return updated == null ? NotFound() : Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        return await productService.DeleteAsync(id) ? NoContent() : NotFound();
    }

    // ─── Variants ─────────────────────────────────────────────────────────────

    [HttpPost("{productId:guid}/variants")]
    public async Task<ActionResult<AdminVariantDto>> AddVariant(Guid productId, [FromBody] AdminVariantWriteDto dto)
        => Ok(await productService.AddVariantAsync(productId, dto));

    [HttpPut("{productId:guid}/variants/{variantId:guid}")]
    public async Task<ActionResult<AdminVariantDto>> UpdateVariant(Guid productId, Guid variantId, [FromBody] AdminVariantWriteDto dto)
    {
        var updated = await productService.UpdateVariantAsync(productId, variantId, dto);
        return updated == null ? NotFound() : Ok(updated);
    }

    [HttpDelete("{productId:guid}/variants/{variantId:guid}")]
    public async Task<IActionResult> DeleteVariant(Guid productId, Guid variantId)
        => await productService.DeleteVariantAsync(productId, variantId) ? NoContent() : NotFound();

    // ─── Images ───────────────────────────────────────────────────────────────

    [HttpPost("{productId:guid}/images")]
    public async Task<ActionResult<AdminProductImageDto>> AddImage(Guid productId, [FromBody] AdminProductImageDto dto)
        => Ok(await productService.AddImageAsync(productId, dto));

    [HttpDelete("{productId:guid}/images/{imageId:guid}")]
    public async Task<IActionResult> DeleteImage(Guid productId, Guid imageId)
        => await productService.DeleteImageAsync(productId, imageId) ? NoContent() : NotFound();
}
