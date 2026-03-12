using App.Domain.ValueObjects;

namespace App.Domain.Venues;

public class Space : BaseEntity
{
    public Guid VenueId { get; set; }
    public Venue Venue { get; set; } = default!;

    public string Name { get; set; } = default!;
    public string Code { get; set; } = default!;
    public string? Description { get; set; }
    public SpaceStatus Status { get; set; } = SpaceStatus.Active;
    public int MinimumBookingDurationMinutes { get; set; } = 60;
    public Money HourlyRate { get; set; } = Money.Zero();
    public CapacityProfile CapacityProfile { get; set; } = CapacityProfile.Empty();
    public Guid? ParentSpaceId { get; set; }
    public Space? ParentSpace { get; set; }

    public ICollection<Space> CombinedSpaces { get; set; } = new List<Space>();
    public ICollection<SpaceLayout> Layouts { get; set; } = new List<SpaceLayout>();
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    public ICollection<EquipmentAllocation> EquipmentAllocations { get; set; } = new List<EquipmentAllocation>();
}
