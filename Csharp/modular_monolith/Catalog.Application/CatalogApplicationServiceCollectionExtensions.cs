using Catalog.Application.Contracts;
using Catalog.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Catalog.Application;

public static class CatalogApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddCatalogApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssemblyContaining<CatalogApplicationAssemblyMarker>());

        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<ICollectionService, CollectionService>();
        services.AddScoped<IAdminProductService, AdminProductService>();
        services.AddScoped<IAdminCatalogueService, AdminCatalogueService>();

        return services;
    }
}

public sealed class CatalogApplicationAssemblyMarker { }
