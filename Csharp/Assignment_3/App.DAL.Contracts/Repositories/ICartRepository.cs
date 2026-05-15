using App.Domain;

namespace App.DAL.EF.Repositories;

public interface ICartRepository : IBaseRepository<Cart>
{
    /// <summary>
    /// The user's active cart with items, variants, colors, sizes, products and product images.
    /// Returns null when the user has no active cart.
    /// </summary>
    Task<Cart?> GetActiveCartForUserAsync(Guid userId);

    /// <summary>
    /// The user's active cart tracked for write, with items and their variants only.
    /// Used by checkout to deduct stock and clear the cart.
    /// </summary>
    Task<Cart?> GetActiveCartForCheckoutAsync(Guid userId);
}
