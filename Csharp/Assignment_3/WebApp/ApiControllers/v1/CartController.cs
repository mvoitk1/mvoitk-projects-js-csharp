using App.BLL.Services;
using App.DTO.v1.Cart;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.Helpers;

namespace WebApp.ApiControllers.v1;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiController]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class CartController(ICartService cartService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(CartDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CartDto>> GetCart()
    {
        var userId = User.UserId();
        return Ok(await cartService.GetOrCreateCartAsync(userId));
    }

    [HttpPost("items")]
    [ProducesResponseType(typeof(CartDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CartDto>> AddItem([FromBody] AddToCartDto dto)
    {
        try
        {
            var userId = User.UserId();
            return Ok(await cartService.AddItemAsync(userId, dto));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("items/{cartItemId:guid}")]
    [ProducesResponseType(typeof(CartDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CartDto>> UpdateItem(Guid cartItemId, [FromBody] UpdateCartItemDto dto)
    {
        try
        {
            var userId = User.UserId();
            return Ok(await cartService.UpdateItemAsync(userId, cartItemId, dto));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("items/{cartItemId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RemoveItem(Guid cartItemId)
    {
        try
        {
            var userId = User.UserId();
            await cartService.RemoveItemAsync(userId, cartItemId);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
