using App.BLL.Contracts;
using App.BLL.DTO.Identity;
using App.DAL.Contracts.UnitOfWork;
using App.Domain.Identity;

namespace App.BLL.Services;

public class IdentityService : IIdentityService
{
    private readonly IAppUnitOfWork _uow;

    public IdentityService(IAppUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task PurgeExpiredRefreshTokensAsync(Guid userId)
    {
        await _uow.RefreshTokens.DeleteExpiredAsync(userId);
    }

    public async Task<string> IssueRefreshTokenAsync(Guid userId, DateTime expiration)
    {
        var token = new AppRefreshToken
        {
            UserId = userId,
            Expiration = expiration
        };
        _uow.RefreshTokens.Add(token);
        await _uow.SaveChangesAsync();
        return token.RefreshToken;
    }

    public async Task<RefreshTokenRotationResult> TryRotateRefreshTokenAsync(
        Guid userId, string currentRefreshToken, DateTime newExpiration)
    {
        var matches = await _uow.RefreshTokens.FindValidAsync(userId, currentRefreshToken);

        if (matches.Count == 0)
            return new RefreshTokenRotationResult { Status = RefreshTokenRotationStatus.NoValidToken };

        if (matches.Count > 1)
            return new RefreshTokenRotationResult { Status = RefreshTokenRotationStatus.AmbiguousToken };

        var token = matches[0];
        // only rotate when the *current* slot matches; if the caller is replaying a previous token, treat as no-op
        if (token.RefreshToken == currentRefreshToken)
        {
            token.PreviousRefreshToken = token.RefreshToken;
            token.PreviousExpiration = DateTime.UtcNow.AddMinutes(1);
            token.RefreshToken = Guid.NewGuid().ToString();
            token.Expiration = newExpiration;
            await _uow.SaveChangesAsync();
        }

        return new RefreshTokenRotationResult
        {
            Status = RefreshTokenRotationStatus.Success,
            NewRefreshToken = token.RefreshToken
        };
    }

    public async Task<int> RevokeRefreshTokenAsync(Guid userId, string refreshToken)
    {
        var matches = await _uow.RefreshTokens.FindValidAsync(userId, refreshToken);
        foreach (var token in matches)
            _uow.RefreshTokens.Remove(token);
        await _uow.SaveChangesAsync();
        return matches.Count;
    }
}
