using Microsoft.AspNetCore.Identity;
using App.Domain.Venues;

namespace App.Domain.Identity;

public class AppUser : IdentityUser<Guid>, IBaseEntity
{
    public ICollection<AppRefreshToken>? RefreshTokens { get; set; }
    public Guid? ActiveVenueId { get; set; }
    public Venue? ActiveVenue { get; set; }
    public ICollection<VenueMembership> VenueMemberships { get; set; } = new List<VenueMembership>();
    public ICollection<VenueAccessRequest> SubmittedVenueAccessRequests { get; set; } = new List<VenueAccessRequest>();
    public ICollection<VenueAccessRequest> ReviewedVenueAccessRequests { get; set; } = new List<VenueAccessRequest>();
    public ICollection<Booking> CreatedBookings { get; set; } = new List<Booking>();
}
