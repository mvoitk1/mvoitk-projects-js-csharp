using App.BLL.DTO.Identity;

namespace App.BLL.Contracts;

/// <summary>
/// Encapsulates refresh-token persistence operations so the API layer doesn't
/// reach into <c>AppDbContext</c> directly. <see cref="UserManager{TUser}"/> /
/// <see cref="SignInManager{TUser}"/> stay in the controller (Identity exception).
/// </summary>
public interface IIdentityService
{
    /// <summary>Delete expired refresh tokens for a user (best-effort cleanup).</summary>
    Task PurgeExpiredRefreshTokensAsync(Guid userId);

    /// <summary>Persist a fresh refresh token for a user; returns the generated token string.</summary>
    Task<string> IssueRefreshTokenAsync(Guid userId, DateTime expiration);

    /// <summary>Find the valid refresh-token row for the user, rotate it into the previous slot, store a fresh token. Reports <c>NoValidToken</c> or <c>AmbiguousToken</c> instead of throwing.</summary>
    Task<RefreshTokenRotationResult> TryRotateRefreshTokenAsync(Guid userId, string currentRefreshToken, DateTime newExpiration);

    /// <summary>Remove every refresh-token row for the user that matches the supplied token (current or previous slot). Returns rows deleted.</summary>
    Task<int> RevokeRefreshTokenAsync(Guid userId, string refreshToken);
}
