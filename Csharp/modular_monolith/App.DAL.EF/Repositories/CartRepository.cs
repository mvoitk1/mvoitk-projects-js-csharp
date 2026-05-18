using App.DAL.Contracts.Repositories;
using Base.DAL.EF;
using App.Domain;
using App.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace App.DAL.EF.Repositories;

public class CartRepository : BaseRepository<Cart, AppDbContext>, ICartRepository
{
    public CartRepository(AppDbContext dbContext) : base(dbContext)
    {
    }

    public async Task<Cart?> GetActiveCartForUserAsync(Guid userId)
    {
        return await RepoDbSet
            .Include(c => c.Items)!
                .ThenInclude(i => i.ProductVariant)
                    .ThenInclude(v => v!.Color)
            .Include(c => c.Items)!
                .ThenInclude(i => i.ProductVariant)
                    .ThenInclude(v => v!.Size)
            .Include(c => c.Items)!
                .ThenInclude(i => i.ProductVariant)
                    .ThenInclude(v => v!.Product)
                        .ThenInclude(p => p!.Images)
            .FirstOrDefaultAsync(c => c.AppUserId == userId && c.Status == CartStatus.Active);
    }

    public async Task<Cart?> GetActiveCartForCheckoutAsync(Guid userId)
    {
        return await RepoDbSet
            .AsTracking()
            .Include(c => c.Items)!
                .ThenInclude(i => i.ProductVariant)
            .FirstOrDefaultAsync(c => c.AppUserId == userId && c.Status == CartStatus.Active);
    }
}
