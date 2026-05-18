using Users.Application.Dtos.Identity;

namespace Users.Application.Contracts;

/// <summary>
/// Encapsulates refresh-token persistence operations so the API layer doesn't
/// reach into the DbContext directly. UserManager/SignInManager stay in the
/// controller (Identity exception).
/// </summary>
public interface IIdentityService
{
    Task PurgeExpiredRefreshTokensAsync(Guid userId);

    Task<string> IssueRefreshTokenAsync(Guid userId, DateTime expiration);

    Task<RefreshTokenRotationResult> TryRotateRefreshTokenAsync(Guid userId, string currentRefreshToken, DateTime newExpiration);

    Task<int> RevokeRefreshTokenAsync(Guid userId, string refreshToken);
}
