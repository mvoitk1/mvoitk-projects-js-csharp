using App.Domain;

namespace App.DAL.EF.Repositories;

public class CartItemRepository : BaseRepository<CartItem>, ICartItemRepository
{
    public CartItemRepository(AppDbContext dbContext) : base(dbContext)
    {
    }

    public void RemoveRange(IEnumerable<CartItem> items)
    {
        RepoDbSet.RemoveRange(items);
    }
}
