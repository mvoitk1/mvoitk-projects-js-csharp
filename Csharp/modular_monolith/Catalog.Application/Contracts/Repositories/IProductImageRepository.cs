using Base.DAL.Contracts;
using Catalog.Domain;

namespace Catalog.Application.Contracts.Repositories;

public interface IProductImageRepository : IBaseRepository<ProductImage>
{
    Task<ProductImage?> FindForProductAsync(Guid productId, Guid imageId);
}
