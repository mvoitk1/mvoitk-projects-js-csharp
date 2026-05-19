using Catalog.Application;
using Catalog.Infrastructure;
using Catalog.Web;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
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

    public void RegisterHealthChecks(IHealthChecksBuilder builder)
    {
        builder.AddDbContextCheck<CatalogDbContext>();
    }

    public Task SeedAsync(IServiceProvider services, IConfiguration configuration, CancellationToken ct)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;
        var logger = sp.GetRequiredService<ILogger<IApplicationBuilder>>();
        var context = sp.GetRequiredService<CatalogDbContext>();

        if (context.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory") return Task.CompletedTask;

        if (configuration.GetValue<bool>("DataInitialization:DropDatabase"))
        {
            logger.LogWarning("DropDatabase: Catalog");
            context.Database.EnsureDeleted();
        }

        if (configuration.GetValue<bool>("DataInitialization:MigrateDatabase"))
        {
            logger.LogInformation("MigrateDatabase: Catalog");
            context.Database.Migrate();
        }

        return Task.CompletedTask;
    }
}
