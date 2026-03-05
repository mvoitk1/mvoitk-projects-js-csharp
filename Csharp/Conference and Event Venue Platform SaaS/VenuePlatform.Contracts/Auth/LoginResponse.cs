namespace VenuePlatform.Contracts.Auth;

public sealed record LoginResponse(string Token, DateTime ExpiresAt, string Email, Guid UserId);
