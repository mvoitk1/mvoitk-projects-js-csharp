using Base.DAL.Contracts;
using Users.Domain;

namespace Users.Application.Contracts;

public interface IRefreshTokenRepository : IBaseRepository<AppRefreshToken>
{
    /// <summary>Delete every refresh-token row for <paramref name="userId"/> whose primary expiration is in the past.</summary>
    Task<int> DeleteExpiredAsync(Guid userId);

    /// <summary>Tokens for <paramref name="userId"/> where <paramref name="refreshToken"/> matches the current or previous slot and the matching expiration is still valid.</summary>
    Task<List<AppRefreshToken>> FindValidAsync(Guid userId, string refreshToken);
}
