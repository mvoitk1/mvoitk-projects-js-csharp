using App.BLL.Contracts;
using App.DTO.Mappers;
using App.DTO.v1.Cart;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.Helpers;
using BllCart = App.BLL.DTO.Cart;

namespace WebApp.Controllers;

[Authorize]
public class CartController(ICartService cartService) : Controller
{
    public async Task<IActionResult> Index()
    {
        var cart = await cartService.GetOrCreateCartAsync(User.UserId());
        return View(cart.MapTo<CartDto>());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddItem(Guid productVariantId, int quantity = 1)
    {
        try
        {
            await cartService.AddItemAsync(User.UserId(), new BllCart.AddToCartDto
            {
                ProductVariantId = productVariantId,
                Quantity = quantity
            });
        }
        catch (InvalidOperationException ex)
        {
            TempData["CartError"] = ex.Message;
        }
        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateItem(Guid cartItemId, int quantity)
    {
        try
        {
            await cartService.UpdateItemAsync(User.UserId(), cartItemId, new BllCart.UpdateCartItemDto { Quantity = quantity });
        }
        catch (InvalidOperationException ex)
        {
            TempData["CartError"] = ex.Message;
        }
        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveItem(Guid cartItemId)
    {
        try
        {
            await cartService.RemoveItemAsync(User.UserId(), cartItemId);
        }
        catch (InvalidOperationException ex)
        {
            TempData["CartError"] = ex.Message;
        }
        return RedirectToAction("Index");
    }
}
