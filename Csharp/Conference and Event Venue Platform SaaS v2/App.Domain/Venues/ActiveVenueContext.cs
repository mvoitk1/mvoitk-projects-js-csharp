namespace App.Domain.Venues;

public class ActiveVenueContext
{
    public Guid UserId { get; init; }
    public Guid VenueId { get; init; }
    public Guid MembershipId { get; init; }
    public Guid CompanyId { get; init; }
    public VenueAccessLevel AccessLevel { get; init; }
    public string VenueName { get; init; } = default!;
    public string CompanyName { get; init; } = default!;
}
