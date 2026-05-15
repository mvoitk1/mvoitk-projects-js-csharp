using App.BLL.Contracts;
using App.BLL.Services;
using Microsoft.Extensions.DependencyInjection;

namespace App.BLL.Setup;

public static class AppBusinessLogicExtensions
{
    public static IServiceCollection AddAppBusinessLogic(this IServiceCollection services)
    {
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<ICollectionService, CollectionService>();
        services.AddScoped<ICartService, CartService>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IAdminProductService, AdminProductService>();
        services.AddScoped<IAdminOrderService, AdminOrderService>();
        services.AddScoped<IAdminCatalogueService, AdminCatalogueService>();
        return services;
    }
}
