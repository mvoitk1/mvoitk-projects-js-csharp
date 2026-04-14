namespace VenuePlatform.Contracts.Reports;

/// <summary>
/// Daily revenue time-series response for a given date range.
/// </summary>
public sealed record DailyRevenueResponse(
    DateTime StartUtc,
    DateTime EndUtc,
    IReadOnlyList<DailyRevenueItem> Days
);
