using App.DTO.v1.Venues.Common;

namespace App.DTO.v1.Venues.Public;

public class PublicSpaceSummaryDto
{
    public Guid SpaceId { get; init; }
    public string Name { get; init; } = default!;
    public string Code { get; init; } = default!;
    public string? Description { get; init; }
    public string Status { get; init; } = default!;
    public int MinimumBookingDurationMinutes { get; init; }
    public MoneyDto HourlyRate { get; init; } = default!;
    public CapacityProfileDto Capacity { get; init; } = default!;
}
