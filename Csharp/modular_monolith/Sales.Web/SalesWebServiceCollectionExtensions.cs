using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.DependencyInjection;

namespace Sales.Web;

public static class SalesWebServiceCollectionExtensions
{
    public static IServiceCollection AddSalesWeb(this IServiceCollection services)
    {
        var assembly = typeof(SalesWebServiceCollectionExtensions).Assembly;
        services.AddControllersWithViews()
            .PartManager.ApplicationParts.Add(new AssemblyPart(assembly));
        return services;
    }
}

public sealed class SalesWebAssemblyMarker { }
