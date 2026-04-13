using App.BLL.Services;
using App.DTO.v1.Admin;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.ApiControllers.v1.Admin;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/admin/collections")]
[ApiController]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")]
public class AdminCollectionsController(IAdminCatalogueService catalogueService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<AdminCollectionDto>>> GetAll()
        => Ok(await catalogueService.GetAllCollectionsAsync());

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AdminCollectionDto>> GetById(Guid id)
    {
        var item = await catalogueService.GetCollectionByIdAsync(id);
        return item == null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<AdminCollectionDto>> Create([FromBody] AdminCollectionWriteDto dto)
    {
        var created = await catalogueService.CreateCollectionAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<AdminCollectionDto>> Update(Guid id, [FromBody] AdminCollectionWriteDto dto)
    {
        var updated = await catalogueService.UpdateCollectionAsync(id, dto);
        return updated == null ? NotFound() : Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
        => await catalogueService.DeleteCollectionAsync(id) ? NoContent() : NotFound();
}
