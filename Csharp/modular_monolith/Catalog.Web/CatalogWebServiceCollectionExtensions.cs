using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.DependencyInjection;

namespace Catalog.Web;

public static class CatalogWebServiceCollectionExtensions
{
    public static IServiceCollection AddCatalogWeb(this IServiceCollection services)
    {
        var assembly = typeof(CatalogWebServiceCollectionExtensions).Assembly;
        services.AddControllersWithViews()
            .PartManager.ApplicationParts.Add(new AssemblyPart(assembly));
        return services;
    }
}

public sealed class CatalogWebAssemblyMarker { }
