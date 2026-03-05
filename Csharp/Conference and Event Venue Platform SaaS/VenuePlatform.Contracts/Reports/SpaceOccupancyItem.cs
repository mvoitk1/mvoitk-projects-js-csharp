namespace VenuePlatform.Contracts.Reports;

/// <summary>
/// Space occupancy details for a single space.
/// </summary>
public sealed record SpaceOccupancyItem(
    Guid SpaceId,
    string SpaceName,
    int BookingCount,
    int TotalBookedMinutes
);
