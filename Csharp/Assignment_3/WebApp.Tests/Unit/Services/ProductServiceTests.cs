using App.BLL.Services;
using WebApp.Tests.Helpers;
using Xunit;

namespace WebApp.Tests.Unit.Services;

public class ProductServiceTests
{
    [Fact]
    public async Task GetListAsync_ExcludesInactiveProducts()
    {
        var db = new TestDatabase();
        var seed = db.SeedCatalog();
        using var ctx = db.NewContext();
        var service = new ProductService(db.NewUnitOfWork(ctx));

        var list = (await service.GetListAsync()).ToList();

        Assert.Equal(2, list.Count);
        Assert.DoesNotContain(list, p => p.Id == seed.InactiveProductId);
    }

    [Fact]
    public async Task GetListAsync_FiltersByCategory()
    {
        var db = new TestDatabase();
        var seed = db.SeedCatalog();
        using var ctx = db.NewContext();
        var service = new ProductService(db.NewUnitOfWork(ctx));

        var list = (await service.GetListAsync(categoryId: seed.CategoryMenId)).ToList();

        Assert.Single(list);
        Assert.Equal(seed.ActiveProductId, list[0].Id);
    }

    [Fact]
    public async Task GetListAsync_FiltersByCollection()
    {
        var db = new TestDatabase();
        var seed = db.SeedCatalog();
        using var ctx = db.NewContext();
        var service = new ProductService(db.NewUnitOfWork(ctx));

        var list = (await service.GetListAsync(collectionId: seed.CollectionBId)).ToList();

        Assert.Single(list);
        Assert.Equal(seed.WomenProductId, list[0].Id);
    }

    [Fact]
    public async Task GetListAsync_FiltersByGender()
    {
        var db = new TestDatabase();
        var seed = db.SeedCatalog();
        using var ctx = db.NewContext();
        var service = new ProductService(db.NewUnitOfWork(ctx));

        var list = (await service.GetListAsync(gender: "Women")).ToList();

        Assert.Single(list);
        Assert.Equal(seed.WomenProductId, list[0].Id);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_ForInactiveProduct()
    {
        var db = new TestDatabase();
        var seed = db.SeedCatalog();
        using var ctx = db.NewContext();
        var service = new ProductService(db.NewUnitOfWork(ctx));

        Assert.Null(await service.GetByIdAsync(seed.InactiveProductId));
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_ForMissingId()
    {
        var db = new TestDatabase();
        db.SeedCatalog();
        using var ctx = db.NewContext();
        var service = new ProductService(db.NewUnitOfWork(ctx));

        Assert.Null(await service.GetByIdAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task GetByIdAsync_MapsVariantsImagesAndCategories()
    {
        var db = new TestDatabase();
        var seed = db.SeedCatalog();
        using var ctx = db.NewContext();
        var service = new ProductService(db.NewUnitOfWork(ctx));

        var dto = await service.GetByIdAsync(seed.ActiveProductId);

        Assert.NotNull(dto);
        Assert.Equal("Classic Tee", dto!.Name);
        Assert.Single(dto.Variants);
        Assert.Equal("TEE-BLK-M", dto.Variants[0].Sku);
        Assert.Equal("Black", dto.Variants[0].ColorName);
        Assert.Equal("M", dto.Variants[0].SizeCode);
        Assert.Single(dto.Images);
        Assert.Contains("Men", dto.CategoryNames);
    }
}
