using App.Domain;

namespace App.DAL.EF.Repositories;

public interface IProductImageRepository : IBaseRepository<ProductImage>
{
    /// <summary>An image scoped to its owning product, or null.</summary>
    Task<ProductImage?> FindForProductAsync(Guid productId, Guid imageId);
}
