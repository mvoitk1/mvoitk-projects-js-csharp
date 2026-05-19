using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Modules.SharedKernel;
using Modules.SharedKernel.Mapping;
using Sales.Application.Contracts;
using Sales.Web.Dtos.v1.Cart;
using Sales.Web.ViewModels;
using AppOrders = Sales.Application.Dtos.Orders;

namespace Sales.Web.Controllers;

[Authorize]
public class CheckoutController(
    ICartService cartService,
    IOrderService orderService) : Controller
{
    public async Task<IActionResult> Index()
    {
        var cart = await cartService.GetOrCreateCartAsync(User.UserId());
        if (!cart.Items.Any())
            return RedirectToAction("Index", "Cart");

        var vm = new CheckoutViewModel { Cart = cart.MapTo<CartDto>() };
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirm(CheckoutViewModel vm)
    {
        vm.Cart = (await cartService.GetOrCreateCartAsync(User.UserId())).MapTo<CartDto>();

        if (!ModelState.IsValid)
            return View("Index", vm);

        try
        {
            var order = await orderService.PlaceOrderAsync(User.UserId(), new AppOrders.CreateOrderDto
            {
                ShippingFirstName = vm.ShippingFirstName,
                ShippingLastName = vm.ShippingLastName,
                ShippingEmail = vm.ShippingEmail,
                ShippingPhone = vm.ShippingPhone,
                ShippingCountry = vm.ShippingCountry,
                ShippingCity = vm.ShippingCity,
                ShippingStreet = vm.ShippingStreet,
                ShippingPostalCode = vm.ShippingPostalCode
            });

            TempData["OrderNumber"] = order.OrderNumber;
            return RedirectToAction("Success");
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View("Index", vm);
        }
    }

    public IActionResult Success()
    {
        ViewData["OrderNumber"] = TempData["OrderNumber"];
        return View();
    }
}
