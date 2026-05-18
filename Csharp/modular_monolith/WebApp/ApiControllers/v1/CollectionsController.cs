using Catalog.Application.Contracts;
using App.DTO.v1.Collections;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.ApiControllers.v1;

/// <summary>Active product collections (curated groupings of products).</summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiController]
public class CollectionsController(ICollectionService collectionService) : ControllerBase
{
    /// <summary>List all active collections.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<CollectionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<CollectionDto>>> GetActive()
    {
        return Ok(await collectionService.GetActiveAsync());
    }
}
