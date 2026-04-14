using App.Domain.Identity;
using App.Domain.ValueObjects;

namespace App.Domain.Venues;

public class Booking : BaseEntity
{
    public Guid VenueId { get; set; }
    public Venue Venue { get; set; } = default!;
    public Guid SpaceId { get; set; }
    public Space Space { get; set; } = default!;
    public Guid CreatedByUserId { get; set; }
    public AppUser CreatedByUser { get; set; } = default!;

    public string Title { get; set; } = default!;
    public string ClientName { get; set; } = default!;
    public BookingStatus Status { get; set; } = BookingStatus.Draft;
    public ScheduleWindow Schedule { get; set; } = default!;
    public int ExpectedAttendees { get; set; }
    public Money SpaceCharge { get; set; } = Money.Zero();
    public string? CoordinationNotes { get; set; }

    public ICollection<CateringOrder> CateringOrders { get; set; } = new List<CateringOrder>();
    public ICollection<EquipmentAllocation> EquipmentAllocations { get; set; } = new List<EquipmentAllocation>();
}
