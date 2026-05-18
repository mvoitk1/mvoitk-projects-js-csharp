using App.BLL.Contracts;
using App.DTO.Mappers;
using App.DTO.v1.Cart;
using Asp.Versioning;
using BllCart = App.BLL.DTO.Cart;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.Helpers;
using Modules.SharedKernel;

namespace WebApp.ApiControllers.v1;

/// <summary>Shopping cart for the authenticated customer.</summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiController]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class CartController(ICartService cartService) : ControllerBase
{
    /// <summary>Get the current user's cart (creates one if it doesn't exist yet).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(CartDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CartDto>> GetCart()
    {
        var userId = User.UserId();
        var cart = await cartService.GetOrCreateCartAsync(userId);
        return Ok(cart.MapTo<CartDto>());
    }

    /// <summary>Add a product variant to the cart.</summary>
    [HttpPost("items")]
    [ProducesResponseType(typeof(CartDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CartDto>> AddItem([FromBody] AddToCartDto dto)
    {
        try
        {
            var userId = User.UserId();
            var cart = await cartService.AddItemAsync(userId, dto.MapTo<BllCart.AddToCartDto>());
            return Ok(cart.MapTo<CartDto>());
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>Update the quantity of a cart line item.</summary>
    /// <param name="cartItemId">Cart item ID to update.</param>
    /// <param name="dto">New cart item data.</param>
    [HttpPut("items/{cartItemId:guid}")]
    [ProducesResponseType(typeof(CartDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CartDto>> UpdateItem(Guid cartItemId, [FromBody] UpdateCartItemDto dto)
    {
        try
        {
            var userId = User.UserId();
            var cart = await cartService.UpdateItemAsync(userId, cartItemId, dto.MapTo<BllCart.UpdateCartItemDto>());
            return Ok(cart.MapTo<CartDto>());
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>Remove a line item from the cart.</summary>
    /// <param name="cartItemId">Cart item ID to remove.</param>
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
