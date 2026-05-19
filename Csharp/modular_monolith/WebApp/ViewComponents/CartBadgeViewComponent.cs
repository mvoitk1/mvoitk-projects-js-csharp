using Sales.Application.Contracts;
using Microsoft.AspNetCore.Mvc;
using WebApp.Helpers;
using Modules.SharedKernel;

namespace WebApp.ViewComponents;

public class CartBadgeViewComponent(ICartService cartService) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        if (UserClaimsPrincipal.Identity?.IsAuthenticated != true)
            return View(0);

        var userId = UserClaimsPrincipal.UserId();
        var cart = await cartService.GetOrCreateCartAsync(userId);
        return View(cart.ItemCount);
    }
}
