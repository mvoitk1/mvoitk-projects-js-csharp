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
}
