using App.BLL.Services;
using App.DTO.v1.Collections;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.ApiControllers.v1;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiController]
public class CollectionsController(ICollectionService collectionService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<CollectionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<CollectionDto>>> GetActive()
    {
        return Ok(await collectionService.GetActiveAsync());
    }
}
