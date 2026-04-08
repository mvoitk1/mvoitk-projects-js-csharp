using App.Domain.ValueObjects;

namespace App.Domain.Venues;

public class CateringOrderLine : BaseEntity
{
    public Guid CateringOrderId { get; set; }
    public CateringOrder CateringOrder { get; set; } = default!;

    public string Name { get; set; } = default!;
    public int Quantity { get; set; }
    public Money UnitPrice { get; set; } = Money.Zero();
    public string? DietaryNotes { get; set; }
}
