using Base.DAL.EF;
using Catalog.Application.Contracts.Repositories;
using Catalog.Domain;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Infrastructure.Repositories;

public class ProductVariantRepository(CatalogDbContext dbContext)
    : BaseRepository<ProductVariant, CatalogDbContext>(dbContext), IProductVariantRepository
{
    public async Task<ProductVariant?> FindActiveAsync(Guid id)
    {
        return await RepoDbSet.FirstOrDefaultAsync(v => v.Id == id && v.IsActive);
    }

    public async Task<ProductVariant?> GetWithColorAndSizeAsync(Guid id)
    {
        return await RepoDbSet
            .Include(v => v.Color)
            .Include(v => v.Size)
            .FirstOrDefaultAsync(v => v.Id == id);
    }

    public async Task<ProductVariant?> GetForProductAsync(Guid productId, Guid variantId)
    {
        return await RepoDbSet
            .Include(v => v.Color)
            .Include(v => v.Size)
            .FirstOrDefaultAsync(v => v.Id == variantId && v.ProductId == productId);
    }

    public async Task<ProductVariant?> FindForProductAsync(Guid productId, Guid variantId)
    {
        return await RepoDbSet
            .FirstOrDefaultAsync(v => v.Id == variantId && v.ProductId == productId);
    }

    public async Task<IEnumerable<ProductVariant>> GetAllActiveWithDetailsAsync()
    {
        return await RepoDbSet
            .Include(v => v.Product)
            .Include(v => v.Color)
            .Include(v => v.Size)
            .Where(v => v.IsActive)
            .OrderBy(v => v.StockQty)
            .ToListAsync();
    }
}
