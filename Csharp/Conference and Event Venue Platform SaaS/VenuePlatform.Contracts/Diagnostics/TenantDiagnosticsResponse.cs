namespace VenuePlatform.Contracts.Diagnostics;

public sealed record TenantDiagnosticsResponse(
    DateTime UtcNow,
    string CompanySlug,
    Guid CompanyId,
    bool DatabaseOk,
    int SpaceCount,
    int BookingCountThisMonth,
    int InvoiceCountThisMonth
);
