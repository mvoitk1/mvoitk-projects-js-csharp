using App.BLL.Contracts;
using App.DTO.Mappers;
using App.DTO.v1.Admin;
using Asp.Versioning;
using BllAdmin = App.BLL.DTO.Admin;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.ApiControllers.v1.Admin;

/// <summary>Back-office collection management. Requires <c>Admin</c> role.</summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/admin/collections")]
[ApiController]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")]
public class AdminCollectionsController(IAdminCatalogueService catalogueService) : ControllerBase
{
    /// <summary>List all collections.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<AdminCollectionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<AdminCollectionDto>>> GetAll()
        => Ok(await catalogueService.GetAllCollectionsAsync());

    /// <summary>Get a collection by ID.</summary>
    /// <param name="id">Collection ID.</param>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(AdminCollectionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminCollectionDto>> GetById(Guid id)
    {
        var item = await catalogueService.GetCollectionByIdAsync(id);
        return item == null ? NotFound() : Ok(item);
    }

    /// <summary>Create a new collection.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(AdminCollectionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AdminCollectionDto>> Create([FromBody] AdminCollectionWriteDto dto)
    {
        var created = await catalogueService.CreateCollectionAsync(dto.MapTo<BllAdmin.AdminCollectionWriteDto>());
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>Update a collection.</summary>
    /// <param name="id">Collection ID.</param>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(AdminCollectionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminCollectionDto>> Update(Guid id, [FromBody] AdminCollectionWriteDto dto)
    {
        var updated = await catalogueService.UpdateCollectionAsync(id, dto.MapTo<BllAdmin.AdminCollectionWriteDto>());
        return updated == null ? NotFound() : Ok(updated);
    }

    /// <summary>Delete a collection.</summary>
    /// <param name="id">Collection ID.</param>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
        => await catalogueService.DeleteCollectionAsync(id) ? NoContent() : NotFound();
}
