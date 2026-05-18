using Asp.Versioning;
using Catalog.Application.Contracts;
using Catalog.Web.Dtos.v1.Collections;
using Microsoft.AspNetCore.Mvc;
using Modules.SharedKernel.Mapping;

namespace Catalog.Web.ApiControllers.v1;

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
        var collections = await collectionService.GetActiveAsync();
        return Ok(collections.Cast<object>().MapList<CollectionDto>());
    }
}
