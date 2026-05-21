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

    public async Task<bool> TryDecrementStockAsync(Guid variantId, int quantity, CancellationToken ct = default)
    {
        if (RepoDbContext.Database.IsRelational())
        {
            // Single atomic guarded UPDATE: the WHERE re-checks stock under a row
            // lock, so concurrent reservations cannot oversell. Runs inside the
            // ambient transaction when the context is enlisted.
            var affected = await RepoDbSet
                .Where(v => v.Id == variantId && v.IsActive && v.StockQty >= quantity)
                .ExecuteUpdateAsync(s => s.SetProperty(v => v.StockQty, v => v.StockQty - quantity), ct);
            return affected > 0;
        }

        // Non-relational provider (InMemory tests): load-check-decrement fallback.
        var variant = await RepoDbSet.FirstOrDefaultAsync(v => v.Id == variantId, ct);
        if (variant is null || !variant.IsActive || variant.StockQty < quantity)
            return false;

        variant.StockQty -= quantity;
        RepoDbSet.Update(variant);
        return true;
    }
}
