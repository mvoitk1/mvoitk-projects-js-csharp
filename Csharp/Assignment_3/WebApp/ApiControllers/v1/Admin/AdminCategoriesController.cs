using App.BLL.Services;
using App.DTO.v1.Admin;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.ApiControllers.v1.Admin;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/admin/categories")]
[ApiController]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")]
public class AdminCategoriesController(IAdminCatalogueService catalogueService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<AdminCategoryDto>>> GetAll()
        => Ok(await catalogueService.GetAllCategoriesAsync());

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AdminCategoryDto>> GetById(Guid id)
    {
        var item = await catalogueService.GetCategoryByIdAsync(id);
        return item == null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<AdminCategoryDto>> Create([FromBody] AdminCategoryWriteDto dto)
    {
        var created = await catalogueService.CreateCategoryAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<AdminCategoryDto>> Update(Guid id, [FromBody] AdminCategoryWriteDto dto)
    {
        var updated = await catalogueService.UpdateCategoryAsync(id, dto);
        return updated == null ? NotFound() : Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
        => await catalogueService.DeleteCategoryAsync(id) ? NoContent() : NotFound();
}
