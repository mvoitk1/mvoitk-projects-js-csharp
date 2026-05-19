using Microsoft.AspNetCore.Mvc;
using Modules.SharedKernel;
using Sales.Application.Contracts;

namespace Sales.Web.ViewComponents;

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
