using App.Domain.ValueObjects;
using App.Domain.Identity;

namespace App.Domain.Venues;

public class Venue : BaseEntity
{
    public Guid CompanyId { get; set; }
    public Company Company { get; set; } = default!;

    public string Name { get; set; } = default!;
    public string Slug { get; set; } = default!;
    public string City { get; set; } = default!;
    public string Country { get; set; } = default!;
    public string AddressLine1 { get; set; } = default!;
    public string? AddressLine2 { get; set; }
    public string? Description { get; set; }
    public VenueLifecycleStatus Status { get; set; } = VenueLifecycleStatus.Draft;
    public Money DefaultHourlyRate { get; set; } = Money.Zero();
    public CapacityProfile CapacityProfile { get; set; } = CapacityProfile.Empty();

    public ICollection<Space> Spaces { get; set; } = new List<Space>();
    public ICollection<VenueMembership> Memberships { get; set; } = new List<VenueMembership>();
    public ICollection<VenueAccessRequest> AccessRequests { get; set; } = new List<VenueAccessRequest>();
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    public ICollection<EquipmentInventoryItem> EquipmentInventory { get; set; } = new List<EquipmentInventoryItem>();
    public ICollection<AppUser> ActiveUsers { get; set; } = new List<AppUser>();
}
