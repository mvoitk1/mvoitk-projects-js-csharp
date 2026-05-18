using Base.DAL.Contracts;
using Sales.Domain;

namespace Sales.Application.Contracts.Repositories;

public interface ICartRepository : IBaseRepository<Cart>
{
    /// <summary>The user's active cart with items only. Variant details come from Catalog via MediatR.</summary>
    Task<Cart?> GetActiveCartForUserAsync(Guid userId);

    /// <summary>Same as above, tracked for write. Used during checkout.</summary>
    Task<Cart?> GetActiveCartForCheckoutAsync(Guid userId);
}
