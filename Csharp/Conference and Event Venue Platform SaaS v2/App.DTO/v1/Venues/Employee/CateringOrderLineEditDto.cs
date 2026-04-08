namespace App.DTO.v1.Venues.Employee;

public class CateringOrderLineEditDto
{
    public Guid? LineId { get; init; }
    public string Name { get; init; } = default!;
    public int Quantity { get; init; }
    public decimal UnitPriceAmount { get; init; }
    public string Currency { get; init; } = "EUR";
    public string? DietaryNotes { get; init; }
}
