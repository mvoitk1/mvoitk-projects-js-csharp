namespace App.DTO.v1.Venues.Admin;

public class UpsertSpaceConfigurationDto
{
    public Guid? SpaceId { get; init; }
    public string Name { get; init; } = default!;
    public string Code { get; init; } = default!;
    public string Status { get; init; } = default!;
    public string? Description { get; init; }
    public int MinimumBookingDurationMinutes { get; init; }
    public decimal HourlyRateAmount { get; init; }
    public string Currency { get; init; } = "EUR";
    public int MinimumCapacity { get; init; }
    public int RecommendedCapacity { get; init; }
    public int MaximumCapacity { get; init; }
    public IReadOnlyList<UpsertSpaceLayoutDto> Layouts { get; init; } = [];
}
