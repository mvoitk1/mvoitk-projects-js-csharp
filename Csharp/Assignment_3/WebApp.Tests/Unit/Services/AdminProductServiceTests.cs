using App.BLL.Services;
using App.DTO.v1.Admin;
using WebApp.Tests.Helpers;
using Xunit;

namespace WebApp.Tests.Unit.Services;

public class AdminProductServiceTests
{
    [Fact]
    public async Task CreateAsync_PersistsProductWithCategories()
    {
        var db = new TestDatabase();
        var seed = db.SeedCatalog();

        Guid createdId;
        using (var ctx = db.NewContext())
        {
            var service = new AdminProductService(db.NewUnitOfWork(ctx));
            var created = await service.CreateAsync(new AdminProductWriteDto
            {
                NameEn = "New Jacket",
                NameEt = "Uus jope",
                Gender = "Unisex",
                IsActive = true,
                CollectionId = seed.CollectionAId,
                CategoryIds = new List<Guid> { seed.CategoryMenId }
            });
            Assert.Equal("New Jacket", created.NameEn);
            Assert.Contains(seed.CategoryMenId, created.CategoryIds);
            createdId = created.Id;
        }

        using var verifyCtx = db.NewContext();
        var fetched = await new AdminProductService(db.NewUnitOfWork(verifyCtx)).GetByIdAsync(createdId);
        Assert.NotNull(fetched);
        Assert.Equal("Uus jope", fetched!.NameEt);
    }

    [Fact]
    public async Task UpdateAsync_ModifiesFieldsAndReplacesCategories()
    {
        var db = new TestDatabase();
        var seed = db.SeedCatalog();

        using (var ctx = db.NewContext())
        {
            var service = new AdminProductService(db.NewUnitOfWork(ctx));
            var updated = await service.UpdateAsync(seed.ActiveProductId, new AdminProductWriteDto
            {
                NameEn = "Renamed Tee",
                NameEt = "Ümbernimetatud särk",
                Gender = "Women",
                IsActive = false,
                CollectionId = seed.CollectionBId,
                CategoryIds = new List<Guid> { seed.CategoryWomenId }
            });
            Assert.NotNull(updated);
            Assert.Equal("Renamed Tee", updated!.NameEn);
            Assert.Equal("Women", updated.Gender);
            Assert.False(updated.IsActive);
        }

        using var verifyCtx = db.NewContext();
        var fetched = await new AdminProductService(db.NewUnitOfWork(verifyCtx)).GetByIdAsync(seed.ActiveProductId);
        Assert.NotNull(fetched);
        Assert.Equal(new[] { seed.CategoryWomenId }, fetched!.CategoryIds);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsNull_ForMissingId()
    {
        var db = new TestDatabase();
        db.SeedCatalog();
        using var ctx = db.NewContext();
        var service = new AdminProductService(db.NewUnitOfWork(ctx));

        var result = await service.UpdateAsync(Guid.NewGuid(), new AdminProductWriteDto { NameEn = "X" });

        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteAsync_RemovesProduct()
    {
        var db = new TestDatabase();
        var seed = db.SeedCatalog();

        bool deleted;
        using (var ctx = db.NewContext())
        {
            var service = new AdminProductService(db.NewUnitOfWork(ctx));
            deleted = await service.DeleteAsync(seed.WomenProductId);
        }

        Assert.True(deleted);
        using var verifyCtx = db.NewContext();
        var fetched = await new AdminProductService(db.NewUnitOfWork(verifyCtx)).GetByIdAsync(seed.WomenProductId);
        Assert.Null(fetched);
    }

    [Fact]
    public async Task AddVariant_ThenDeleteVariant_RoundTrips()
    {
        var db = new TestDatabase();
        var seed = db.SeedCatalog();

        Guid variantId;
        using (var ctx = db.NewContext())
        {
            var service = new AdminProductService(db.NewUnitOfWork(ctx));
            var variant = await service.AddVariantAsync(seed.ActiveProductId, new AdminVariantWriteDto
            {
                Sku = "TEE-BLK-L",
                Price = 31.99m,
                UnitPrice = 13.00m,
                StockQty = 7,
                IsActive = true,
                ColorId = seed.ColorId,
                SizeId = seed.SizeId
            });
            Assert.Equal("TEE-BLK-L", variant.Sku);
            Assert.Equal("Black", variant.ColorName);
            variantId = variant.Id;
        }

        using var deleteCtx = db.NewContext();
        var deleted = await new AdminProductService(db.NewUnitOfWork(deleteCtx))
            .DeleteVariantAsync(seed.ActiveProductId, variantId);
        Assert.True(deleted);
    }

    [Fact]
    public async Task AddImage_ThenDeleteImage_RoundTrips()
    {
        var db = new TestDatabase();
        var seed = db.SeedCatalog();

        Guid imageId;
        using (var ctx = db.NewContext())
        {
            var service = new AdminProductService(db.NewUnitOfWork(ctx));
            var image = await service.AddImageAsync(seed.ActiveProductId, new AdminProductImageDto
            {
                Url = "https://example.test/extra.jpg",
                AltTextEn = "Extra photo",
                AltTextEt = "Lisafoto",
                SortOrder = 1
            });
            Assert.Equal("https://example.test/extra.jpg", image.Url);
            Assert.Equal("Extra photo", image.AltTextEn);
            imageId = image.Id;
        }

        using var deleteCtx = db.NewContext();
        var deleted = await new AdminProductService(db.NewUnitOfWork(deleteCtx))
            .DeleteImageAsync(seed.ActiveProductId, imageId);
        Assert.True(deleted);
    }
}
