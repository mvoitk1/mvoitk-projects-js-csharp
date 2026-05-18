using Base.DAL.EF;
using Sales.Application.Contracts.Repositories;
using Sales.Domain;

namespace Sales.Infrastructure.Repositories;

public class CartItemRepository(SalesDbContext dbContext)
    : BaseRepository<CartItem, SalesDbContext>(dbContext), ICartItemRepository
{
    public void RemoveRange(IEnumerable<CartItem> items)
    {
        RepoDbSet.RemoveRange(items);
    }
}
