using Base.DAL.Contracts;
using App.Domain;

namespace App.DAL.Contracts.Repositories;

public interface IProductImageRepository : IBaseRepository<ProductImage>
{
    /// <summary>An image scoped to its owning product, or null.</summary>
    Task<ProductImage?> FindForProductAsync(Guid productId, Guid imageId);
}
