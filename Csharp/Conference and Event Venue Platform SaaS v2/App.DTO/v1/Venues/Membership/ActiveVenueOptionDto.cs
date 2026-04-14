namespace App.DTO.v1.Venues.Membership;

public class ActiveVenueOptionDto
{
    public Guid MembershipId { get; init; }
    public Guid VenueId { get; init; }
    public string VenueName { get; init; } = default!;
    public string CompanyName { get; init; } = default!;
    public string AccessLevel { get; init; } = default!;
    public string VenueStatus { get; init; } = default!;
    public string MembershipStatus { get; init; } = default!;
    public bool IsCurrentVenue { get; init; }
    public bool IsDefaultVenue { get; init; }
    public bool CanSelect { get; init; }
}
