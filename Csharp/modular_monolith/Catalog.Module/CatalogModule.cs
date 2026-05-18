using Catalog.Application;
using Catalog.Infrastructure;
using Catalog.Web;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Modules.Abstractions;

namespace Catalog.Module;

public sealed class CatalogModule : IModule
{
    public string Name => "Catalog";

    public void Register(IServiceCollection services, IConfiguration configuration)
    {
        services.AddCatalogInfrastructure(configuration);
        services.AddCatalogApplication();
        services.AddCatalogWeb();
    }
}
