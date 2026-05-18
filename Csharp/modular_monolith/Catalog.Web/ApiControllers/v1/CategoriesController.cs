using Asp.Versioning;
using Catalog.Application.Contracts;
using Catalog.Web.Dtos.v1.Categories;
using Microsoft.AspNetCore.Mvc;
using Modules.SharedKernel.Mapping;

namespace Catalog.Web.ApiControllers.v1;

/// <summary>Product categories used to organise the shop catalogue.</summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiController]
public class CategoriesController(ICategoryService categoryService) : ControllerBase
{
    /// <summary>List all categories.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<CategoryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<CategoryDto>>> GetAll()
    {
        var categories = await categoryService.GetAllAsync();
        return Ok(categories.Cast<object>().MapList<CategoryDto>());
    }
}
