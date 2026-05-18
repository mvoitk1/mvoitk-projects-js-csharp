using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Modules.Abstractions;
using Sales.Application;
using Sales.Infrastructure;
using Sales.Web;

namespace Sales.Module;

public sealed class SalesModule : IModule
{
    public string Name => "Sales";

    public void Register(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSalesInfrastructure(configuration);
        services.AddSalesApplication();
        services.AddSalesWeb();
    }
}
