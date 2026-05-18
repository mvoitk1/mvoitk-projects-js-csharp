using App.DAL.EF.Repositories;
using App.Domain;
using App.Domain.Enums;
using WebApp.Tests.Helpers;
using Xunit;

namespace WebApp.Tests.Unit.Repositories;

public class OrderRepositoryTests
{
    private static Order NewOrder(Guid userId, string number) => new()
    {
        OrderNumber = number,
        Status = OrderStatus.Confirmed,
        TotalAmount = 10m,
        AppUserId = userId
    };

    [Fact]
    public async Task GetUserOrderByIdAsync_ReturnsNull_WhenOrderBelongsToAnotherUser()
    {
        var db = new TestDatabase();
        var ownerId = Guid.NewGuid();
        Guid orderId;
        using (var seedCtx = db.NewContext())
        {
            var order = NewOrder(ownerId, "ORD-1");
            seedCtx.Orders.Add(order);
            seedCtx.SaveChanges();
            orderId = order.Id;
        }

        using var ctx = db.NewContext();
        var repo = new OrderRepository(ctx);

        Assert.Null(await repo.GetUserOrderByIdAsync(Guid.NewGuid(), orderId));
    }

    [Fact]
    public async Task GetUserOrderByIdAsync_ReturnsOrder_ForOwner()
    {
        var db = new TestDatabase();
        var ownerId = Guid.NewGuid();
        Guid orderId;
        using (var seedCtx = db.NewContext())
        {
            var order = NewOrder(ownerId, "ORD-2");
            seedCtx.Orders.Add(order);
            seedCtx.SaveChanges();
            orderId = order.Id;
        }

        using var ctx = db.NewContext();
        var repo = new OrderRepository(ctx);

        var result = await repo.GetUserOrderByIdAsync(ownerId, orderId);

        Assert.NotNull(result);
        Assert.Equal("ORD-2", result!.OrderNumber);
    }

    [Fact]
    public async Task GetUserOrdersAsync_ReturnsOnlyTheGivenUsersOrders()
    {
        var db = new TestDatabase();
        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();
        using (var seedCtx = db.NewContext())
        {
            seedCtx.Orders.AddRange(
                NewOrder(userA, "A-1"),
                NewOrder(userA, "A-2"),
                NewOrder(userB, "B-1"));
            seedCtx.SaveChanges();
        }

        using var ctx = db.NewContext();
        var repo = new OrderRepository(ctx);

        var result = (await repo.GetUserOrdersAsync(userA)).ToList();

        Assert.Equal(2, result.Count);
        Assert.All(result, o => Assert.Equal(userA, o.AppUserId));
    }
}
