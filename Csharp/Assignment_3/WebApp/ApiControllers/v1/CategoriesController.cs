using App.BLL.Services;
using App.DTO.v1.Categories;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.ApiControllers.v1;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiController]
public class CategoriesController(ICategoryService categoryService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<CategoryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<CategoryDto>>> GetAll()
    {
        return Ok(await categoryService.GetAllAsync());
    }
}
