using Base.DAL.Contracts;
using App.Domain;

namespace App.DAL.Contracts.Repositories;

public interface IProductVariantRepository : IBaseRepository<ProductVariant>
{
    /// <summary>An active variant by id, or null. Used when adding to cart.</summary>
    Task<ProductVariant?> FindActiveAsync(Guid id);

    /// <summary>A variant with its colour and size loaded.</summary>
    Task<ProductVariant?> GetWithColorAndSizeAsync(Guid id);

    /// <summary>A variant scoped to its owning product, with colour and size loaded.</summary>
    Task<ProductVariant?> GetForProductAsync(Guid productId, Guid variantId);

    /// <summary>A variant scoped to its owning product, without includes.</summary>
    Task<ProductVariant?> FindForProductAsync(Guid productId, Guid variantId);

    /// <summary>All active variants with product, colour and size loaded, ordered by stock ascending.</summary>
    Task<IEnumerable<ProductVariant>> GetAllActiveWithDetailsAsync();
}
