namespace VenuePlatform.Contracts.Companies;

public sealed record CreateCompanyResponse(
    Guid CompanyId,
    string CompanyName,
    string CompanySlug,
    string Role
);
