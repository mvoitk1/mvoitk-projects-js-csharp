using System.Linq;
using System.Threading.Tasks;
using App.DAL.EF;
using App.DAL.EF.Seeding;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
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

    private AppDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_container.GetConnectionString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task Migrations_ApplyOnEmptyDatabase_Succeed()
    {
        await using var db = NewContext();

        await db.Database.MigrateAsync();

        var applied = (await db.Database.GetAppliedMigrationsAsync()).ToList();
        Assert.NotEmpty(applied);
    }

    [Fact]
    public async Task SeedData_RunsAfterMigrations()
    {
        await using (var db = NewContext())
        {
            await db.Database.MigrateAsync();
            AppDataInit.SeedAppData(db);
        }

        await using var verify = NewContext();
        Assert.True(await verify.Products.AnyAsync());
        Assert.True(await verify.Categories.AnyAsync());
    }
}
