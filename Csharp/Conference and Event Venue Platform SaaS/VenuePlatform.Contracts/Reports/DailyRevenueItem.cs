namespace VenuePlatform.Contracts.Reports;

/// <summary>
/// Revenue details for a single UTC day.
/// </summary>
public sealed record DailyRevenueItem(
    DateOnly DateUtc,
    decimal TotalPayments,
    int PaymentCount
);
