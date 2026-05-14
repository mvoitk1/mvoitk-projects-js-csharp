using App.DAL.EF.Repositories;
using App.Domain.Enums;
using WebApp.Tests.Helpers;
using Xunit;

namespace WebApp.Tests.Unit.Repositories;

public class ProductRepositoryTests
{
    [Fact]
    public async Task GetListFilteredAsync_ReturnsOnlyActiveProducts()
    {
        var db = new TestDatabase();
        var seed = db.SeedCatalog();
        using var ctx = db.NewContext();
        var repo = new ProductRepository(ctx);

        var products = (await repo.GetListFilteredAsync()).ToList();

        Assert.Equal(2, products.Count);
        Assert.DoesNotContain(products, p => p.Id == seed.InactiveProductId);
    }

    [Fact]
    public async Task GetListFilteredAsync_LoadsTheFullIncludeGraph()
    {
        var db = new TestDatabase();
        var seed = db.SeedCatalog();
        using var ctx = db.NewContext();
        var repo = new ProductRepository(ctx);

        var products = await repo.GetListFilteredAsync(categoryId: seed.CategoryMenId);
        var product = Assert.Single(products);

        Assert.NotNull(product.Variants);
        Assert.NotEmpty(product.Variants);
        Assert.NotNull(product.Images);
        Assert.NotEmpty(product.Images);
        Assert.NotNull(product.Collection);
        Assert.NotNull(product.ProductCategories);
        Assert.NotEmpty(product.ProductCategories);
    }

    [Fact]
    public async Task GetListFilteredAsync_FiltersByGender()
    {
        var db = new TestDatabase();
        var seed = db.SeedCatalog();
        using var ctx = db.NewContext();
        var repo = new ProductRepository(ctx);

        var products = await repo.GetListFilteredAsync(gender: Gender.Women);
        var product = Assert.Single(products);

        Assert.Equal(seed.WomenProductId, product.Id);
    }

    [Fact]
    public async Task GetWithDetailsAsync_ActiveOnly_ExcludesInactiveProduct()
    {
        var db = new TestDatabase();
        var seed = db.SeedCatalog();
        using var ctx = db.NewContext();
        var repo = new ProductRepository(ctx);

        Assert.Null(await repo.GetWithDetailsAsync(seed.InactiveProductId, activeOnly: true));
        Assert.NotNull(await repo.GetWithDetailsAsync(seed.InactiveProductId, activeOnly: false));
    }
}
