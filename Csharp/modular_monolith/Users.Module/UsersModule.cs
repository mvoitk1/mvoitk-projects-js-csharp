using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Modules.Abstractions;
using Users.Application;
using Users.Domain;
using Users.Infrastructure;
using Users.Infrastructure.Seeding;
using Users.Web;

namespace Users.Module;

public sealed class UsersModule : IModule
{
    public string Name => "Users";

    public void Register(IServiceCollection services, IConfiguration configuration)
    {
        services.AddUsersInfrastructure(configuration);
        services.AddUsersApplication();
        services.AddUsersWeb();
    }

    public void RegisterHealthChecks(IHealthChecksBuilder builder)
    {
        builder.AddDbContextCheck<UsersDbContext>();
    }

    public async Task SeedAsync(IServiceProvider services, IConfiguration configuration, CancellationToken ct)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;
        var logger = sp.GetRequiredService<ILogger<IApplicationBuilder>>();
        var context = sp.GetRequiredService<UsersDbContext>();

        if (context.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory") return;

        // Users runs first in module composition order: wait for the DB to be reachable
        // before any other module attempts to migrate or seed.
        UsersDataInit.WaitDbConnection(context, logger);

        if (configuration.GetValue<bool>("DataInitialization:DropDatabase"))
        {
            logger.LogWarning("DropDatabase");
            UsersDataInit.DeleteDatabase(context);
        }

        if (configuration.GetValue<bool>("DataInitialization:MigrateDatabase"))
        {
            logger.LogInformation("MigrateDatabase: Users");
            UsersDataInit.MigrateDatabase(context);
        }

        if (configuration.GetValue<bool>("DataInitialization:SeedIdentity"))
        {
            logger.LogInformation("SeedIdentity");
            var userManager = sp.GetRequiredService<UserManager<AppUser>>();
            var roleManager = sp.GetRequiredService<RoleManager<AppRole>>();
            await UsersDataInit.SeedIdentityAsync(userManager, roleManager);
        }
    }
}
