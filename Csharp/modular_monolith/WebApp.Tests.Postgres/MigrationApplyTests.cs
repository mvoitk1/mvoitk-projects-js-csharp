using System.Linq;
using System.Threading.Tasks;
using Catalog.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Sales.Infrastructure;
using Testcontainers.PostgreSql;
using Users.Infrastructure;
using Xunit;

namespace WebApp.Tests.Postgres;

public class MigrationApplyTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("migration_test")
        .WithUsername("test")
        .WithPassword("test")
        .Build();

    public Task InitializeAsync() => _container.StartAsync();

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();

    private TContext NewContext<TContext>() where TContext : DbContext
    {
        var options = new DbContextOptionsBuilder<TContext>()
            .UseNpgsql(_container.GetConnectionString())
            .Options;
        return (TContext)System.Activator.CreateInstance(typeof(TContext), options)!;
    }

    [Fact]
    public async Task UsersMigrations_ApplyOnEmptyDatabase_Succeed()
    {
        await using var db = NewContext<UsersDbContext>();
        await db.Database.MigrateAsync();
        Assert.NotEmpty(await db.Database.GetAppliedMigrationsAsync());
    }

    [Fact]
    public async Task CatalogMigrations_ApplyOnEmptyDatabase_Succeed()
    {
        await using (var users = NewContext<UsersDbContext>())
        {
            await users.Database.MigrateAsync();
        }

        await using var db = NewContext<CatalogDbContext>();
        await db.Database.MigrateAsync();
        Assert.NotEmpty(await db.Database.GetAppliedMigrationsAsync());
    }

    [Fact]
    public async Task SalesMigrations_ApplyOnEmptyDatabase_Succeed()
    {
        await using (var users = NewContext<UsersDbContext>())
        {
            await users.Database.MigrateAsync();
        }

        await using var db = NewContext<SalesDbContext>();
        await db.Database.MigrateAsync();
        Assert.NotEmpty(await db.Database.GetAppliedMigrationsAsync());
    }
}
