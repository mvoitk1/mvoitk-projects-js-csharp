using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
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

    public void RegisterHealthChecks(IHealthChecksBuilder builder)
    {
        builder.AddDbContextCheck<SalesDbContext>();
    }

    public Task SeedAsync(IServiceProvider services, IConfiguration configuration, CancellationToken ct)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;
        var logger = sp.GetRequiredService<ILogger<IApplicationBuilder>>();
        var context = sp.GetRequiredService<SalesDbContext>();

        if (context.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory") return Task.CompletedTask;

        if (configuration.GetValue<bool>("DataInitialization:DropDatabase"))
        {
            logger.LogWarning("DropDatabase: Sales");
            context.Database.EnsureDeleted();
        }

        if (configuration.GetValue<bool>("DataInitialization:MigrateDatabase"))
        {
            logger.LogInformation("MigrateDatabase: Sales");
            context.Database.Migrate();
        }

        return Task.CompletedTask;
    }
}
