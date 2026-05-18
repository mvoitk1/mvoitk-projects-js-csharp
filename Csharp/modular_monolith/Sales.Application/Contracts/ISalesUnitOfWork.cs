using Sales.Application.Contracts.Repositories;

namespace Sales.Application.Contracts;

public interface ISalesUnitOfWork
{
    ICartRepository Carts { get; }
    ICartItemRepository CartItems { get; }
    IOrderRepository Orders { get; }

    Task<int> SaveChangesAsync();
}
