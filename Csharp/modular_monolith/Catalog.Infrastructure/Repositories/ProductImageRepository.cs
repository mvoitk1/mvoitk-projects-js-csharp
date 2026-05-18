using Base.DAL.EF;
using Catalog.Application.Contracts.Repositories;
using Catalog.Domain;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Infrastructure.Repositories;

public class ProductImageRepository(CatalogDbContext dbContext)
    : BaseRepository<ProductImage, CatalogDbContext>(dbContext), IProductImageRepository
{
    public async Task<ProductImage?> FindForProductAsync(Guid productId, Guid imageId)
    {
        return await RepoDbSet
            .FirstOrDefaultAsync(i => i.Id == imageId && i.ProductId == productId);
    }
}
