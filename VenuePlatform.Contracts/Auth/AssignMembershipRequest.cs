namespace VenuePlatform.Contracts.Auth;

public sealed record AssignMembershipRequest(Guid UserId, Guid CompanyId, string Role);
