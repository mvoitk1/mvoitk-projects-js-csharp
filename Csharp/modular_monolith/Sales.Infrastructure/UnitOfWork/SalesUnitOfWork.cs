using Sales.Application.Contracts;
using Sales.Application.Contracts.Repositories;
using Sales.Infrastructure.Repositories;

namespace Sales.Infrastructure.UnitOfWork;

public class SalesUnitOfWork : ISalesUnitOfWork
{
    private readonly SalesDbContext _dbContext;

    public SalesUnitOfWork(SalesDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    private ICartRepository? _carts;
    public ICartRepository Carts => _carts ??= new CartRepository(_dbContext);

    private ICartItemRepository? _cartItems;
    public ICartItemRepository CartItems => _cartItems ??= new CartItemRepository(_dbContext);

    private IOrderRepository? _orders;
    public IOrderRepository Orders => _orders ??= new OrderRepository(_dbContext);

    public Task<int> SaveChangesAsync() => _dbContext.SaveChangesAsync();
}
