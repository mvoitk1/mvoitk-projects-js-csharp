namespace App.Domain.Venues;

public class Company : BaseEntity
{
    public string Name { get; set; } = default!;
    public string RegistrationCode { get; set; } = default!;
    public string ContactEmail { get; set; } = default!;
    public string? ContactPhone { get; set; }
    public string? Notes { get; set; }

    public ICollection<Venue> Venues { get; set; } = new List<Venue>();
    public ICollection<VenueMembership> Memberships { get; set; } = new List<VenueMembership>();
    public ICollection<VenueAccessRequest> AccessRequests { get; set; } = new List<VenueAccessRequest>();
}
