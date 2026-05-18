using Microsoft.Extensions.DependencyInjection;

namespace Catalog.Application;

public static class CatalogApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddCatalogApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssemblyContaining<CatalogApplicationAssemblyMarker>());
        // Service registrations land here as catalog services are migrated.
        return services;
    }
}

public sealed class CatalogApplicationAssemblyMarker { }
