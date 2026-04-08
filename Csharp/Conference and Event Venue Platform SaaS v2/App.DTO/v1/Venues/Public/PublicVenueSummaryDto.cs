using App.DTO.v1.Venues.Common;

namespace App.DTO.v1.Venues.Public;

public class PublicVenueSummaryDto
{
    public Guid VenueId { get; init; }
    public string Name { get; init; } = default!;
    public string Slug { get; init; } = default!;
    public string City { get; init; } = default!;
    public string Country { get; init; } = default!;
    public string AddressLine1 { get; init; } = default!;
    public string? Description { get; init; }
    public MoneyDto DefaultHourlyRate { get; init; } = default!;
    public CapacityProfileDto Capacity { get; init; } = default!;
    public int SpaceCount { get; init; }
    public int UpcomingBookingsCount { get; init; }
}
