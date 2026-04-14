namespace VenuePlatform.Contracts.Auth;

/// <summary>
/// Response after successful user registration.
/// </summary>
public sealed record RegisterResponse(
    Guid UserId,
    string Email,
    string? CompanySlug,
    string? MembershipRole
);
