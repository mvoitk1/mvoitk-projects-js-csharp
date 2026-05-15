using App.DAL.Contracts.UnitOfWork;
using App.DAL.EF;
using App.DAL.EF.UnitOfWork;
using Microsoft.EntityFrameworkCore;

namespace WebApp.Tests.Helpers;

/// <summary>
/// One isolated in-memory database for a single unit test. Hands out fresh
/// <see cref="AppDbContext"/> instances over the same data so tests can seed in one
/// context and exercise the code under test in another — exactly how the real app
/// gets a new scoped context per request. Sharing a single context between arrange
/// and act would hide / distort EF change-tracking behaviour.
/// </summary>
public sealed class TestDatabase
{
    private readonly string _databaseName = $"test-db-{Guid.NewGuid()}";

    public AppDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(_databaseName)
            .Options;

        var ctx = new TestAppDbContext(options);
        ctx.Database.EnsureCreated();
        return ctx;
    }

    public IAppUnitOfWork NewUnitOfWork(AppDbContext ctx) => new AppUnitOfWork(ctx);

    /// <summary>Seed the shared catalogue fixture in its own context.</summary>
    public SeededCatalog SeedCatalog()
    {
        using var ctx = NewContext();
        return TestData.SeedCatalog(ctx);
    }
}
