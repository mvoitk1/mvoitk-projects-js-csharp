namespace Users.Application.Dtos.Identity;

public enum RefreshTokenRotationStatus
{
    Success,
    NoValidToken,
    AmbiguousToken
}

public class RefreshTokenRotationResult
{
    public RefreshTokenRotationStatus Status { get; init; }
    public string? NewRefreshToken { get; init; }
}
