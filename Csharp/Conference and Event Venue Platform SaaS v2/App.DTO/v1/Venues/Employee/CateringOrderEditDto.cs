namespace App.DTO.v1.Venues.Employee;

public class CateringOrderEditDto
{
    public int GuestCount { get; init; }
    public string? Notes { get; init; }
    public IReadOnlyList<CateringOrderLineEditDto> Lines { get; init; } = [];
}
