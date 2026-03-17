namespace App.DTO.v1.Venues.Membership;

public class ActiveVenueSelectionResultDto
{
    public Guid VenueId { get; init; }
    public Guid MembershipId { get; init; }
    public string VenueName { get; init; } = default!;
    public string CompanyName { get; init; } = default!;
    public string AccessLevel { get; init; } = default!;
    public string VenueStatus { get; init; } = default!;
    public bool IsRejectedVenue { get; init; }
    public string? RejectionNotes { get; init; }
}
