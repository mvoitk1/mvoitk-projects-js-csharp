namespace VenuePlatform.Contracts.Reports;

/// <summary>
/// Space occupancy summary response for a given date range.
/// </summary>
public sealed record SpaceOccupancyResponse(
    DateTime StartUtc,
    DateTime EndUtc,
    int TotalBookings,
    IReadOnlyList<SpaceOccupancyItem> Spaces
);
