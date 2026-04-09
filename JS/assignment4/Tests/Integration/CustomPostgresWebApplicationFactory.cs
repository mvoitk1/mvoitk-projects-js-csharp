using App.DAL.EF;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;
using WebApp.Tests.Helpers;

namespace Tests.Integration;

/// <summary>
/// Custom WebApplicationFactory that uses a real PostgreSQL database for integration testing.
/// This enables proper testing of PostgreSQL-specific features like JSONB columns (used by LangStr).
/// </summary>
/// <typeparam name="TStartup">The entry point class of the application (typically Program)</typeparam>
public class CustomWebApplicationFactory<TStartup>
    : WebApplicationFactory<TStartup> where TStartup : class
{
    private const string TestDatabaseName = "WebApp_TESTING";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Enable Npgsql dynamic JSON mapping for LangStr support
        // This must be called before any database connections are made
#pragma warning disable CS0618 // Type or member is obsolete
        NpgsqlConnection.GlobalTypeMapper.EnableDynamicJson();
#pragma warning restore CS0618 // Type or member is obsolete

        builder.ConfigureServices(services =>
        {
            // Find and remove the existing DbContextOptions registration
            var descriptorDbContextOptions =
                services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));

            if (descriptorDbContextOptions != null)
            {
                services.Remove(descriptorDbContextOptions);
            }

            // Also remove the DbContext registration if present
            var descriptorDbContext = services.SingleOrDefault(d => d.ServiceType == typeof(AppDbContext));

            if (descriptorDbContext != null)
            {
                services.Remove(descriptorDbContext);
            }

            // Get the connection string from configuration and modify database name
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false)
                .Build();

            var connectionString = configuration.GetConnectionString("DefaultConnection")
                                   ?? throw new InvalidOperationException(
                                       "Connection string 'DefaultConnection' not found.");

            // Replace the database name with test database name
            var testConnectionString = ReplaceDatabase(connectionString, TestDatabaseName);

            // Add DbContext with PostgreSQL provider for testing
            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseNpgsql(testConnectionString,
                        npgsqlOptions =>
                        {
                            npgsqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                        })
                    .ConfigureWarnings(w =>
                        w.Throw(RelationalEventId.MultipleCollectionIncludeWarning)
                    );
            });

            // Build service provider and set up the database
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var scopedServices = scope.ServiceProvider;
            var db = scopedServices.GetRequiredService<AppDbContext>();
            var logger = scopedServices.GetRequiredService<ILogger<CustomWebApplicationFactory<TStartup>>>();

            try
            {
                // Drop the database to ensure clean state for each test run
                logger.LogInformation("Dropping test database: {DatabaseName}", TestDatabaseName);
                db.Database.EnsureDeleted();

                // Apply migrations to create fresh schema
                logger.LogInformation("Applying migrations to test database: {DatabaseName}", TestDatabaseName);
                db.Database.Migrate();

                // Seed identity first (users and roles)
                logger.LogInformation("Seeding identity data");
                DataSeeder.SeedIdentityData(scopedServices);

                // Then seed app data (organizations, maps, floors, etc.)
                logger.LogInformation("Seeding application data");
                DataSeeder.SeedData(db);

                logger.LogInformation("Test database setup complete");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred setting up the test database. Error: {Message}", ex.Message);
                throw;
            }
        });
    }

    /// <summary>
    /// Replaces the database name in a PostgreSQL connection string.
    /// </summary>
    /// <param name="connectionString">The original connection string</param>
    /// <param name="newDatabaseName">The new database name to use</param>
    /// <returns>Modified connection string with new database name</returns>
    private static string ReplaceDatabase(string connectionString, string newDatabaseName)
    {
        var builder = new NpgsqlConnectionStringBuilder(connectionString)
        {
            Database = newDatabaseName
        };
        return builder.ToString();
    }
}