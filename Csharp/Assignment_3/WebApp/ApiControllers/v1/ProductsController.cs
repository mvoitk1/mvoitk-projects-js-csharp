using App.BLL.Services;
using App.DTO.v1.Products;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.ApiControllers.v1;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiController]
public class ProductsController(IProductService productService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ProductListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ProductListItemDto>>> GetAll(
        [FromQuery] Guid? categoryId,
        [FromQuery] Guid? collectionId,
        [FromQuery] string? gender)
    {
        var products = await productService.GetListAsync(categoryId, collectionId, gender);
        return Ok(products);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDto>> GetById(Guid id)
    {
        var product = await productService.GetByIdAsync(id);
        if (product == null) return NotFound();
        return Ok(product);
    }
}
