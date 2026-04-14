using VenuePlatform.Contracts.Billing;

namespace VenuePlatform.Contracts.Companies;

public sealed record PublicCompanyDto(
    string CompanySlug,
    string CompanyName,
    CompanyPlan Plan
);
