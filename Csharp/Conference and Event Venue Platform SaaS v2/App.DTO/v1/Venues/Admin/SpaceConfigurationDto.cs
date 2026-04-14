using App.DTO.v1.Venues.Common;

namespace App.DTO.v1.Venues.Admin;

public class SpaceConfigurationDto
{
    public Guid SpaceId { get; init; }
    public string Name { get; init; } = default!;
    public string Code { get; init; } = default!;
    public string Status { get; init; } = default!;
    public string? Description { get; init; }
    public int MinimumBookingDurationMinutes { get; init; }
    public MoneyDto HourlyRate { get; init; } = default!;
    public CapacityProfileDto Capacity { get; init; } = default!;
    public IReadOnlyList<SpaceLayoutDto> Layouts { get; init; } = [];
}
