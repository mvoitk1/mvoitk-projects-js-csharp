namespace VenuePlatform.Contracts.Reports;

/// <summary>
/// Daily space occupancy details for a single day and space.
/// </summary>
public sealed record DailySpaceOccupancyItem(
    DateOnly DateUtc,
    Guid SpaceId,
    string SpaceName,
    int BookingCount,
    int TotalBookedMinutes
);
