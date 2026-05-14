using App.DAL.EF.Repositories;
using App.Domain;
using App.Domain.Enums;
using WebApp.Tests.Helpers;
using Xunit;

namespace WebApp.Tests.Unit.Repositories;

public class CartRepositoryTests
{
    [Fact]
    public async Task GetActiveCartForUserAsync_IgnoresNonActiveCarts()
    {
        var db = new TestDatabase();
        var userId = Guid.NewGuid();
        using (var seedCtx = db.NewContext())
        {
            seedCtx.Carts.Add(new Cart { AppUserId = userId, Status = CartStatus.CheckedOut });
            seedCtx.SaveChanges();
        }

        using var ctx = db.NewContext();
        var repo = new CartRepository(ctx);

        Assert.Null(await repo.GetActiveCartForUserAsync(userId));
    }

    [Fact]
    public async Task GetActiveCartForUserAsync_ReturnsActiveCartWithItemGraph()
    {
        var db = new TestDatabase();
        var seed = db.SeedCatalog();
        var userId = Guid.NewGuid();
        using (var seedCtx = db.NewContext())
            TestData.SeedCartWithItem(seedCtx, userId, seed.ActiveVariantId, quantity: 2, unitPrice: 29.99m);

        using var ctx = db.NewContext();
        var repo = new CartRepository(ctx);

        var cart = await repo.GetActiveCartForUserAsync(userId);

        Assert.NotNull(cart);
        Assert.Equal(CartStatus.Active, cart!.Status);
        var item = Assert.Single(cart.Items);
        Assert.NotNull(item.ProductVariant);
        Assert.NotNull(item.ProductVariant!.Color);
        Assert.NotNull(item.ProductVariant.Size);
        Assert.NotNull(item.ProductVariant.Product);
    }

    [Fact]
    public async Task GetActiveCartForUserAsync_ReturnsActiveCart_WhenUserAlsoHasCheckedOutCart()
    {
        var db = new TestDatabase();
        var userId = Guid.NewGuid();
        Guid activeCartId;
        using (var seedCtx = db.NewContext())
        {
            seedCtx.Carts.Add(new Cart { AppUserId = userId, Status = CartStatus.CheckedOut });
            var active = new Cart { AppUserId = userId, Status = CartStatus.Active };
            seedCtx.Carts.Add(active);
            seedCtx.SaveChanges();
            activeCartId = active.Id;
        }

        using var ctx = db.NewContext();
        var repo = new CartRepository(ctx);

        var cart = await repo.GetActiveCartForUserAsync(userId);

        Assert.NotNull(cart);
        Assert.Equal(activeCartId, cart!.Id);
    }
}
