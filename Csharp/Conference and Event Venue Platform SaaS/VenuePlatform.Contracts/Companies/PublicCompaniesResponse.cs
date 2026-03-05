namespace VenuePlatform.Contracts.Companies;

public sealed record PublicCompaniesResponse(
    List<PublicCompanyDto> Companies
);
