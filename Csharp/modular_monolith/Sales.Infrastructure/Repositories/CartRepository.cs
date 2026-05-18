using Base.DAL.EF;
using Microsoft.EntityFrameworkCore;
using Sales.Application.Contracts.Repositories;
using Sales.Domain;
using Sales.Domain.Enums;

namespace Sales.Infrastructure.Repositories;

public class CartRepository(SalesDbContext dbContext)
    : BaseRepository<Cart, SalesDbContext>(dbContext), ICartRepository
{
    public async Task<Cart?> GetActiveCartForUserAsync(Guid userId)
    {
        return await RepoDbSet
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.AppUserId == userId && c.Status == CartStatus.Active);
    }

    public async Task<Cart?> GetActiveCartForCheckoutAsync(Guid userId)
    {
        return await RepoDbSet
            .AsTracking()
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.AppUserId == userId && c.Status == CartStatus.Active);
    }
}
