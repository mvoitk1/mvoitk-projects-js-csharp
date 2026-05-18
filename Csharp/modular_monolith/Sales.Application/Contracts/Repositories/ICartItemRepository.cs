using Base.DAL.Contracts;
using Sales.Domain;

namespace Sales.Application.Contracts.Repositories;

public interface ICartItemRepository : IBaseRepository<CartItem>
{
    void RemoveRange(IEnumerable<CartItem> items);
}
