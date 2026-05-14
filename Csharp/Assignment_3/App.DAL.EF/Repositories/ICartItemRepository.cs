using App.Domain;

namespace App.DAL.EF.Repositories;

public interface ICartItemRepository : IBaseRepository<CartItem>
{
    void RemoveRange(IEnumerable<CartItem> items);
}
