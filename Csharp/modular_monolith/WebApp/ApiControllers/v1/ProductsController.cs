using Catalog.Application.Contracts;
using App.DTO.Mappers;
using App.DTO.v1.Products;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.ApiControllers.v1;

/// <summary>Products available in the shop.</summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiController]
public class ProductsController(IProductService productService) : ControllerBase
{
    /// <summary>List all products, optionally filtered by category, collection, or gender.</summary>
    /// <param name="categoryId">Filter by category ID.</param>
    /// <param name="collectionId">Filter by collection ID.</param>
    /// <param name="gender">Filter by gender slug (e.g. <c>men</c>, <c>women</c>, <c>unisex</c>).</param>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ProductListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ProductListItemDto>>> GetAll(
        [FromQuery] Guid? categoryId,
        [FromQuery] Guid? collectionId,
        [FromQuery] string? gender)
    {
        var products = await productService.GetListAsync(categoryId, collectionId, gender);
        return Ok(products.Cast<object>().MapList<ProductListItemDto>());
    }

    /// <summary>Get full product details including variants and images.</summary>
    /// <param name="id">Product ID.</param>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDto>> GetById(Guid id)
    {
        var product = await productService.GetByIdAsync(id);
        if (product == null) return NotFound();
        return Ok(product.MapTo<ProductDto>());
    }
}
