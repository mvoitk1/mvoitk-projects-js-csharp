using App.Domain.Identity;

namespace App.Domain.Venues;

public class VenueMembership : BaseEntity
{
    public Guid CompanyId { get; set; }
    public Company Company { get; set; } = default!;
    public Guid VenueId { get; set; }
    public Venue Venue { get; set; } = default!;
    public Guid UserId { get; set; }
    public AppUser User { get; set; } = default!;

    public VenueAccessLevel AccessLevel { get; set; }
    public VenueMembershipStatus Status { get; set; } = VenueMembershipStatus.PendingActivation;
    public bool IsDefaultVenue { get; set; }
    public DateTime GrantedAt { get; set; } = DateTime.UtcNow;
    public DateTime? BlockedAt { get; set; }
    public string? Notes { get; set; }
}
