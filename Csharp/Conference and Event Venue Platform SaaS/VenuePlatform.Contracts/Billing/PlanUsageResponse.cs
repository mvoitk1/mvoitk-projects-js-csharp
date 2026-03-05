namespace VenuePlatform.Contracts.Billing;

/// <summary>
/// Response containing tenant plan, limits, and current usage.
/// </summary>
public sealed record PlanUsageResponse(
    string CompanyName,
    string CompanySlug,
    CompanyPlan Plan,
    int MaxSpaces,
    int CurrentSpaces,
    int MaxBookingsPerMonth,
    int CurrentBookingsThisMonth,
    DateTime MonthStartUtc,
    DateTime MonthEndUtc
);
