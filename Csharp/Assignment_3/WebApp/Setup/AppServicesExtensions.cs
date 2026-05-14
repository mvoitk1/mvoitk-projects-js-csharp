using App.BLL.Services;
using App.DAL.EF.UnitOfWork;
using Microsoft.Extensions.DependencyInjection;

namespace WebApp.Setup;

public static class AppServicesExtensions
{
    public static IServiceCollection AddAppServices(this IServiceCollection services)
    {
        // Data-access seam: BLL services depend on IAppUnitOfWork, never on AppDbContext.
        services.AddScoped<IAppUnitOfWork, AppUnitOfWork>();

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
