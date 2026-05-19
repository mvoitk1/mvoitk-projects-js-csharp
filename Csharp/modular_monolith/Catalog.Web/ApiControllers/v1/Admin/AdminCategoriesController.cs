using Catalog.Application.Contracts;
using Modules.SharedKernel.Mapping;
using Catalog.Web.Dtos.v1.Admin;
using Asp.Versioning;
using BllAdmin = Catalog.Application.Dtos.Admin;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Catalog.Web.ApiControllers.v1.Admin;

/// <summary>Back-office category management. Requires <c>Admin</c> role.</summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/admin/categories")]
[ApiController]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")]
public class AdminCategoriesController(IAdminCatalogueService catalogueService) : ControllerBase
{
    /// <summary>List all categories.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<AdminCategoryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<AdminCategoryDto>>> GetAll()
        => Ok(await catalogueService.GetAllCategoriesAsync());

    /// <summary>Get a category by ID.</summary>
    /// <param name="id">Category ID.</param>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(AdminCategoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminCategoryDto>> GetById(Guid id)
    {
        var item = await catalogueService.GetCategoryByIdAsync(id);
        return item == null ? NotFound() : Ok(item);
    }

    /// <summary>Create a new category.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(AdminCategoryDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AdminCategoryDto>> Create([FromBody] AdminCategoryWriteDto dto)
    {
        var created = await catalogueService.CreateCategoryAsync(dto.MapTo<BllAdmin.AdminCategoryWriteDto>());
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>Update a category.</summary>
    /// <param name="id">Category ID.</param>
    /// <param name="dto">Updated category data.</param>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(AdminCategoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminCategoryDto>> Update(Guid id, [FromBody] AdminCategoryWriteDto dto)
    {
        var updated = await catalogueService.UpdateCategoryAsync(id, dto.MapTo<BllAdmin.AdminCategoryWriteDto>());
        return updated == null ? NotFound() : Ok(updated);
    }

    /// <summary>Delete a category.</summary>
    /// <param name="id">Category ID.</param>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
        => await catalogueService.DeleteCategoryAsync(id) ? NoContent() : NotFound();
}
