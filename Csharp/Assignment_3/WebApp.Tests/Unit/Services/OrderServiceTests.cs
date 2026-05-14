using App.BLL.Services;
using App.Domain;
using App.Domain.Enums;
using App.DTO.v1.Orders;
using WebApp.Tests.Helpers;
using Xunit;

namespace WebApp.Tests.Unit.Services;

public class OrderServiceTests
{
    private static CreateOrderDto SampleShipping() => new()
    {
        ShippingFirstName = "Madis",
        ShippingLastName = "Voitk",
        ShippingEmail = "madis@shop.test",
        ShippingPhone = "+372 5000 0000",
        ShippingCountry = "Estonia",
        ShippingCity = "Tallinn",
        ShippingStreet = "Akadeemia tee 1",
        ShippingPostalCode = "12345"
    };

    [Fact]
    public async Task PlaceOrderAsync_Throws_WhenNoActiveCart()
    {
        var db = new TestDatabase();
        db.SeedCatalog();
        using var ctx = db.NewContext();
        var service = new OrderService(db.NewUnitOfWork(ctx));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.PlaceOrderAsync(Guid.NewGuid(), SampleShipping()));
    }

    [Fact]
    public async Task PlaceOrderAsync_Throws_OnInsufficientStock()
    {
        var db = new TestDatabase();
        var seed = db.SeedCatalog();
        var userId = Guid.NewGuid();
        using (var seedCtx = db.NewContext())
            TestData.SeedCartWithItem(seedCtx, userId, seed.ActiveVariantId, quantity: 999, unitPrice: 29.99m);

        using var ctx = db.NewContext();
        var service = new OrderService(db.NewUnitOfWork(ctx));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.PlaceOrderAsync(userId, SampleShipping()));
    }

    [Fact]
    public async Task PlaceOrderAsync_DeductsStock_ClearsCart_AndChecksOut()
    {
        var db = new TestDatabase();
        var seed = db.SeedCatalog();
        var userId = Guid.NewGuid();
        Guid cartId;
        using (var seedCtx = db.NewContext())
            cartId = TestData.SeedCartWithItem(seedCtx, userId, seed.ActiveVariantId, quantity: 3, unitPrice: 29.99m).Id;

        using (var ctx = db.NewContext())
        {
            var service = new OrderService(db.NewUnitOfWork(ctx));
            var order = await service.PlaceOrderAsync(userId, SampleShipping());

            Assert.Equal("Confirmed", order.Status);
            Assert.Equal(3 * 29.99m, order.TotalAmount);
            Assert.Single(order.Items);
        }

        using var verifyCtx = db.NewContext();
        var variant = await verifyCtx.ProductVariants.FindAsync(seed.ActiveVariantId);
        Assert.Equal(7, variant!.StockQty); // 10 - 3

        var reloadedCart = await verifyCtx.Carts.FindAsync(cartId);
        Assert.Equal(CartStatus.CheckedOut, reloadedCart!.Status);
        Assert.Empty(verifyCtx.CartItems.Where(ci => ci.CartId == cartId));
    }

    [Fact]
    public async Task GetUserOrderByIdAsync_ReturnsNull_ForAnotherUsersOrder()
    {
        var db = new TestDatabase();
        var ownerId = Guid.NewGuid();
        var attackerId = Guid.NewGuid();
        Guid orderId;
        using (var seedCtx = db.NewContext())
        {
            var order = new Order
            {
                OrderNumber = "ORD-OWNED",
                Status = OrderStatus.Confirmed,
                TotalAmount = 10m,
                AppUserId = ownerId
            };
            seedCtx.Orders.Add(order);
            seedCtx.SaveChanges();
            orderId = order.Id;
        }

        using var ctx = db.NewContext();
        var service = new OrderService(db.NewUnitOfWork(ctx));

        Assert.Null(await service.GetUserOrderByIdAsync(attackerId, orderId));
        Assert.NotNull(await service.GetUserOrderByIdAsync(ownerId, orderId));
    }
}
