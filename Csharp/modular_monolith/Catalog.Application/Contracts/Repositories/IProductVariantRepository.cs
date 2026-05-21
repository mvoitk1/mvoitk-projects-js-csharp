using Base.DAL.Contracts;
using Catalog.Domain;

namespace Catalog.Application.Contracts.Repositories;

public interface IProductVariantRepository : IBaseRepository<ProductVariant>
{
    Task<ProductVariant?> FindActiveAsync(Guid id);
    Task<ProductVariant?> GetWithColorAndSizeAsync(Guid id);
    Task<ProductVariant?> GetForProductAsync(Guid productId, Guid variantId);
    Task<ProductVariant?> FindForProductAsync(Guid productId, Guid variantId);
    Task<IEnumerable<ProductVariant>> GetAllActiveWithDetailsAsync();

    /// <summary>
    /// Atomically decrements stock for an active variant only if at least
    /// <paramref name="quantity"/> is available. Returns true on success, false
    /// when the variant is missing, inactive, or short on stock. Safe under
    /// concurrency — the check and decrement are a single guarded statement.
    /// </summary>
    Task<bool> TryDecrementStockAsync(Guid variantId, int quantity, CancellationToken ct = default);
}
