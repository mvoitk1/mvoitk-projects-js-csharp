using Base.DAL.Contracts;
using App.Domain;

namespace App.DAL.Contracts.Repositories;

public interface ICartItemRepository : IBaseRepository<CartItem>
{
    void RemoveRange(IEnumerable<CartItem> items);
}
