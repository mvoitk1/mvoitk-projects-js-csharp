using System;
using System.Linq;
using Catalog.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Sales.Infrastructure;
using Users.Infrastructure;

namespace WebApp.Tests;

public class CustomWebApplicationFactory<TStartup>
    : WebApplicationFactory<TStartup> where TStartup: class
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            ReplaceDbContext<UsersDbContext>(services, "InMemoryDbForTesting_Users");
            ReplaceDbContext<CatalogDbContext>(services, "InMemoryDbForTesting_Catalog");
            ReplaceDbContext<SalesDbContext>(services, "InMemoryDbForTesting_Sales");

            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            scope.ServiceProvider.GetRequiredService<UsersDbContext>().Database.EnsureCreated();
            scope.ServiceProvider.GetRequiredService<CatalogDbContext>().Database.EnsureCreated();
            scope.ServiceProvider.GetRequiredService<SalesDbContext>().Database.EnsureCreated();
        });
    }

    private static void ReplaceDbContext<TContext>(IServiceCollection services, string dbName)
        where TContext : DbContext
    {
        var descriptor = services.SingleOrDefault(
            d => d.ServiceType == typeof(DbContextOptions<TContext>));
        if (descriptor != null) services.Remove(descriptor);

        var options = new DbContextOptionsBuilder<TContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        services.AddScoped(_ => options);
    }
}
