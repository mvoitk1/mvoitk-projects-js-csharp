namespace VenuePlatform.Contracts.Auth;

public sealed record MeResponse(
    Guid UserId,
    string Email,
    bool HasCompanies,
    List<UserCompanyDto> Companies
);
