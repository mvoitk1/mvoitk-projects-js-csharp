using System.Threading;
using Catalog.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sales.Infrastructure;
using Users.Domain;
using Users.Infrastructure;
using Users.Infrastructure.Seeding;

namespace WebApp.Setup;

public static class AppDataInitExtensions
{
    public static async Task SetupAppDataAsync(this WebApplication app)
    {
        using var serviceScope = app.Services
            .GetRequiredService<IServiceScopeFactory>()
            .CreateScope();
        var logger = serviceScope.ServiceProvider.GetRequiredService<ILogger<IApplicationBuilder>>();

        var usersContext = serviceScope.ServiceProvider.GetRequiredService<UsersDbContext>();
        var catalogContext = serviceScope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        var salesContext = serviceScope.ServiceProvider.GetRequiredService<SalesDbContext>();

        if (usersContext.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory") return;

        WaitDbConnection(usersContext, logger);

        var configuration = app.Configuration;

        if (configuration.GetValue<bool>("DataInitialization:DropDatabase"))
        {
            logger.LogWarning("DropDatabase");
            salesContext.Database.EnsureDeleted();
            catalogContext.Database.EnsureDeleted();
            UsersDataInit.DeleteDatabase(usersContext);
        }

        if (configuration.GetValue<bool>("DataInitialization:MigrateDatabase"))
        {
            logger.LogInformation("MigrateDatabase");
            // Users schema must come first (other modules reference user IDs).
            UsersDataInit.MigrateDatabase(usersContext);
            catalogContext.Database.Migrate();
            salesContext.Database.Migrate();
        }

        if (configuration.GetValue<bool>("DataInitialization:SeedIdentity"))
        {
            logger.LogInformation("SeedIdentity");
            var userManager = serviceScope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            var roleManager = serviceScope.ServiceProvider.GetRequiredService<RoleManager<AppRole>>();
            await UsersDataInit.SeedIdentityAsync(userManager, roleManager);
        }
    }

    private static void WaitDbConnection(DbContext ctx, ILogger<IApplicationBuilder> logger)
    {
        while (true)
        {
            try
            {
                ctx.Database.OpenConnection();
                ctx.Database.CloseConnection();
                return;
            }
            catch (Npgsql.PostgresException e)
            {
                logger.LogWarning("Checked postgres db connection. Got: {Msg}", e.Message);

                if (e.Message.Contains("does not exist"))
                {
                    logger.LogWarning("Applying migration, probably db is not there (but server is)");
                    return;
                }

                logger.LogWarning("Waiting for db connection. Sleep 1 sec");
                Thread.Sleep(1000);
            }
            catch (Npgsql.NpgsqlException e)
            {
                logger.LogWarning("Waiting for db (network error): {Msg}", e.Message);
                Thread.Sleep(1000);
            }
        }
    }
}
