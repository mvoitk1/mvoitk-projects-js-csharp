using App.BLL.Services;
using Microsoft.Extensions.DependencyInjection;

namespace WebApp.Setup;

public static class AppServicesExtensions
{
    public static IServiceCollection AddAppServices(this IServiceCollection services)
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
