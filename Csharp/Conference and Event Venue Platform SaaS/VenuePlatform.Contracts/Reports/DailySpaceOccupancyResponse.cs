namespace VenuePlatform.Contracts.Reports;

/// <summary>
/// Daily space occupancy time-series response for a given date range.
/// </summary>
public sealed record DailySpaceOccupancyResponse(
    DateTime StartUtc,
    DateTime EndUtc,
    IReadOnlyList<DailySpaceOccupancyItem> Items
);
