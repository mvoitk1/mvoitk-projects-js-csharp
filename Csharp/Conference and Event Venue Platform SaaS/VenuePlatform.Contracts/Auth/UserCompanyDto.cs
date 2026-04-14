namespace VenuePlatform.Contracts.Auth;

public sealed record UserCompanyDto(
    string CompanySlug,
    string CompanyName,
    string Role
);
