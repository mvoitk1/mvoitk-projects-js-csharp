namespace VenuePlatform.Contracts.Reports;

/// <summary>
/// Revenue summary response for a given date range.
/// </summary>
public sealed record RevenueSummaryResponse(
    DateTime StartUtc,
    DateTime EndUtc,
    decimal TotalPayments,
    int PaymentCount
);
