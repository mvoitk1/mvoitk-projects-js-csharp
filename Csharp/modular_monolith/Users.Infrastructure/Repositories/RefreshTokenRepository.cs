using Base.DAL.EF;
using Microsoft.EntityFrameworkCore;
using Users.Application.Contracts;
using Users.Domain;

namespace Users.Infrastructure.Repositories;

public class RefreshTokenRepository(UsersDbContext dbContext)
    : BaseRepository<AppRefreshToken, UsersDbContext>(dbContext), IRefreshTokenRepository
{
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
