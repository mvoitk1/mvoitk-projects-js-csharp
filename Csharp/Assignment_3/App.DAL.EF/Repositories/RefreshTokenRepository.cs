using App.DAL.Contracts.Repositories;
using App.Domain.Identity;
using Base.DAL.EF;
using Microsoft.EntityFrameworkCore;

namespace App.DAL.EF.Repositories;

public class RefreshTokenRepository : BaseRepository<AppRefreshToken, AppDbContext>, IRefreshTokenRepository
{
    public RefreshTokenRepository(AppDbContext dbContext) : base(dbContext)
    {
    }

    public Task<int> DeleteExpiredAsync(Guid userId)
    {
        if (!RepoDbContext.Database.ProviderName!.Contains("InMemory"))
        {
            return RepoDbSet
                .Where(t => t.UserId == userId && t.Expiration < DateTime.UtcNow)
                .ExecuteDeleteAsync();
        }

        return Task.FromResult(0);
    }

    public async Task<List<AppRefreshToken>> FindValidAsync(Guid userId, string refreshToken)
    {
        return await RepoDbSet
            .Where(t => t.UserId == userId &&
                        ((t.RefreshToken == refreshToken && t.Expiration > DateTime.UtcNow) ||
                         (t.PreviousRefreshToken == refreshToken &&
                          t.PreviousExpiration > DateTime.UtcNow)))
            .ToListAsync();
    }
}
