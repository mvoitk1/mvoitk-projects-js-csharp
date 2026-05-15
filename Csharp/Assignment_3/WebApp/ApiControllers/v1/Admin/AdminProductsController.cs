using App.BLL.Contracts;
using App.DTO.Mappers;
using App.DTO.v1.Admin;
using Asp.Versioning;
using BllAdmin = App.BLL.DTO.Admin;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.ApiControllers.v1.Admin;

/// <summary>Back-office product management. Requires <c>Admin</c> role.</summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/admin/products")]
[ApiController]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")]
public class AdminProductsController(IAdminProductService productService) : ControllerBase
{
    /// <summary>List all products (including inactive).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<AdminProductDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<AdminProductDto>>> GetAll()
        => Ok(await productService.GetAllAsync());

    /// <summary>Get a product by ID.</summary>
    /// <param name="id">Product ID.</param>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(AdminProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminProductDto>> GetById(Guid id)
    {
        var product = await productService.GetByIdAsync(id);
        return product == null ? NotFound() : Ok(product);
    }

    /// <summary>Create a new product.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(AdminProductDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AdminProductDto>> Create([FromBody] AdminProductWriteDto dto)
    {
        var created = await productService.CreateAsync(dto.MapTo<BllAdmin.AdminProductWriteDto>());
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>Update an existing product.</summary>
    /// <param name="id">Product ID.</param>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(AdminProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminProductDto>> Update(Guid id, [FromBody] AdminProductWriteDto dto)
    {
        var updated = await productService.UpdateAsync(id, dto.MapTo<BllAdmin.AdminProductWriteDto>());
        return updated == null ? NotFound() : Ok(updated);
    }

    /// <summary>Delete a product.</summary>
    /// <param name="id">Product ID.</param>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
        => await productService.DeleteAsync(id) ? NoContent() : NotFound();

    // ─── Variants ─────────────────────────────────────────────────────────────

    /// <summary>Add a size/colour variant to a product.</summary>
    /// <param name="productId">Product ID.</param>
    [HttpPost("{productId:guid}/variants")]
    [ProducesResponseType(typeof(AdminVariantDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminVariantDto>> AddVariant(Guid productId, [FromBody] AdminVariantWriteDto dto)
        => Ok(await productService.AddVariantAsync(productId, dto.MapTo<BllAdmin.AdminVariantWriteDto>()));

    /// <summary>Update a product variant.</summary>
    /// <param name="productId">Product ID.</param>
    /// <param name="variantId">Variant ID.</param>
    [HttpPut("{productId:guid}/variants/{variantId:guid}")]
    [ProducesResponseType(typeof(AdminVariantDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminVariantDto>> UpdateVariant(Guid productId, Guid variantId, [FromBody] AdminVariantWriteDto dto)
    {
        var updated = await productService.UpdateVariantAsync(productId, variantId, dto.MapTo<BllAdmin.AdminVariantWriteDto>());
        return updated == null ? NotFound() : Ok(updated);
    }

    /// <summary>Delete a product variant.</summary>
    /// <param name="productId">Product ID.</param>
    /// <param name="variantId">Variant ID.</param>
    [HttpDelete("{productId:guid}/variants/{variantId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteVariant(Guid productId, Guid variantId)
        => await productService.DeleteVariantAsync(productId, variantId) ? NoContent() : NotFound();

    // ─── Images ───────────────────────────────────────────────────────────────

    /// <summary>Add an image URL to a product.</summary>
    /// <param name="productId">Product ID.</param>
    [HttpPost("{productId:guid}/images")]
    [ProducesResponseType(typeof(AdminProductImageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminProductImageDto>> AddImage(Guid productId, [FromBody] AdminProductImageDto dto)
        => Ok(await productService.AddImageAsync(productId, dto.MapTo<BllAdmin.AdminProductImageDto>()));

    /// <summary>Remove a product image.</summary>
    /// <param name="productId">Product ID.</param>
    /// <param name="imageId">Image ID.</param>
    [HttpDelete("{productId:guid}/images/{imageId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteImage(Guid productId, Guid imageId)
        => await productService.DeleteImageAsync(productId, imageId) ? NoContent() : NotFound();
}
