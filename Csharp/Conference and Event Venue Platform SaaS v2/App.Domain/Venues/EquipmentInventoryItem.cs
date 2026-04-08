using App.Domain.ValueObjects;

namespace App.Domain.Venues;

public class EquipmentInventoryItem : BaseEntity
{
    public Guid VenueId { get; set; }
    public Venue Venue { get; set; } = default!;

    public string Name { get; set; } = default!;
    public string Category { get; set; } = default!;
    public int TotalQuantity { get; set; }
    public EquipmentInventoryStatus Status { get; set; } = EquipmentInventoryStatus.Available;
    public Money UnitPrice { get; set; } = Money.Zero();

    public ICollection<EquipmentAllocation> Allocations { get; set; } = new List<EquipmentAllocation>();
}
