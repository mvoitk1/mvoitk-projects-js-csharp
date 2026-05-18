using App.BLL.Services;
using App.BLL.Contracts;
using App.BLL.DTO.Cart;
using WebApp.Tests.Helpers;
using Xunit;

namespace WebApp.Tests.Unit.Services;

public class CartServiceTests
{
    [Fact]
    public async Task AddItemAsync_CreatesCartAndAddsItem()
    {
        var db = new TestDatabase();
        var seed = db.SeedCatalog();
        var userId = Guid.NewGuid();
        using var ctx = db.NewContext();
        var service = new CartService(db.NewUnitOfWork(ctx));

        var cart = await service.AddItemAsync(userId,
            new AddToCartDto { ProductVariantId = seed.ActiveVariantId, Quantity = 2 });

        Assert.Single(cart.Items);
        Assert.Equal(2, cart.Items[0].Quantity);
        Assert.Equal("TEE-BLK-M", cart.Items[0].Sku);
    }

    [Fact]
    public async Task AddItemAsync_IncrementsQuantity_WhenItemAlreadyInCart()
    {
        var db = new TestDatabase();
        var seed = db.SeedCatalog();
        var userId = Guid.NewGuid();
        using var ctx = db.NewContext();
        var service = new CartService(db.NewUnitOfWork(ctx));

        await service.AddItemAsync(userId,
            new AddToCartDto { ProductVariantId = seed.ActiveVariantId, Quantity = 2 });
        var cart = await service.AddItemAsync(userId,
            new AddToCartDto { ProductVariantId = seed.ActiveVariantId, Quantity = 3 });

        Assert.Single(cart.Items);
        Assert.Equal(5, cart.Items[0].Quantity);
    }

    [Fact]
    public async Task AddItemAsync_Throws_OnInsufficientStock()
    {
        var db = new TestDatabase();
        var seed = db.SeedCatalog();
        using var ctx = db.NewContext();
        var service = new CartService(db.NewUnitOfWork(ctx));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.AddItemAsync(Guid.NewGuid(),
                new AddToCartDto { ProductVariantId = seed.ActiveVariantId, Quantity = 999 }));
    }

    [Fact]
    public async Task UpdateItemAsync_ChangesQuantity()
    {
        var db = new TestDatabase();
        var seed = db.SeedCatalog();
        var userId = Guid.NewGuid();
        using var ctx = db.NewContext();
        var service = new CartService(db.NewUnitOfWork(ctx));

        var cart = await service.AddItemAsync(userId,
            new AddToCartDto { ProductVariantId = seed.ActiveVariantId, Quantity = 1 });

        var updated = await service.UpdateItemAsync(userId, cart.Items[0].Id,
            new UpdateCartItemDto { Quantity = 4 });

        Assert.Equal(4, updated.Items[0].Quantity);
    }

    [Fact]
    public async Task RemoveItemAsync_RemovesItemFromCart()
    {
        var db = new TestDatabase();
        var seed = db.SeedCatalog();
        var userId = Guid.NewGuid();
        using var ctx = db.NewContext();
        var service = new CartService(db.NewUnitOfWork(ctx));

        var cart = await service.AddItemAsync(userId,
            new AddToCartDto { ProductVariantId = seed.ActiveVariantId, Quantity = 1 });

        await service.RemoveItemAsync(userId, cart.Items[0].Id);

        var reloaded = await service.GetOrCreateCartAsync(userId);
        Assert.Empty(reloaded.Items);
    }
}
