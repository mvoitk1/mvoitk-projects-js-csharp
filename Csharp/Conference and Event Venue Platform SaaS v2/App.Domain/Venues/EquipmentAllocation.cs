using App.Domain.ValueObjects;

namespace App.Domain.Venues;

public class EquipmentAllocation : BaseEntity
{
    public Guid BookingId { get; set; }
    public Booking Booking { get; set; } = default!;
    public Guid EquipmentInventoryItemId { get; set; }
    public EquipmentInventoryItem EquipmentInventoryItem { get; set; } = default!;
    public Guid? SpaceId { get; set; }
    public Space? Space { get; set; }

    public int Quantity { get; set; }
    public EquipmentAllocationStatus Status { get; set; } = EquipmentAllocationStatus.Reserved;
    public ScheduleWindow Schedule { get; set; } = default!;
    public Money TotalPrice { get; set; } = Money.Zero();
    public string? Notes { get; set; }
}
