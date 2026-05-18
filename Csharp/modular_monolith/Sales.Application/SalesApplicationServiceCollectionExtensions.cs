using Microsoft.Extensions.DependencyInjection;
using Sales.Application.Contracts;
using Sales.Application.Services;

namespace Sales.Application;

public static class SalesApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddSalesApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssemblyContaining<SalesApplicationAssemblyMarker>());

        services.AddScoped<ICartService, CartService>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IAdminOrderService, AdminOrderService>();
        return services;
    }
}

public sealed class SalesApplicationAssemblyMarker { }
