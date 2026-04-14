using App.DTO.v1.Venues.Common;

namespace App.DTO.v1.Venues.Employee;

public class CateringOrderLineDto
{
    public Guid LineId { get; init; }
    public string Name { get; init; } = default!;
    public int Quantity { get; init; }
    public MoneyDto UnitPrice { get; init; } = default!;
    public string? DietaryNotes { get; init; }
}
