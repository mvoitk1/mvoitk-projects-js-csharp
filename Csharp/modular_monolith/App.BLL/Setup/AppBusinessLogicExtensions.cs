using App.BLL.Contracts;
using App.BLL.Services;
using Microsoft.Extensions.DependencyInjection;

namespace App.BLL.Setup;

public static class AppBusinessLogicExtensions
{
    public static IServiceCollection AddAppBusinessLogic(this IServiceCollection services)
    {
        services.AddScoped<ICartService, CartService>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IAdminOrderService, AdminOrderService>();
        return services;
    }
}
