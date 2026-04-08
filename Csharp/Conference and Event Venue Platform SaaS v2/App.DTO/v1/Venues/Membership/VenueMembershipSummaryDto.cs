namespace App.DTO.v1.Venues.Membership;

public class VenueMembershipSummaryDto
{
    public Guid MembershipId { get; init; }
    public Guid UserId { get; init; }
    public Guid VenueId { get; init; }
    public Guid CompanyId { get; init; }
    public string VenueName { get; init; } = default!;
    public string CompanyName { get; init; } = default!;
    public string AccessLevel { get; init; } = default!;
    public string Status { get; init; } = default!;
    public bool IsDefaultVenue { get; init; }
    public DateTime GrantedAt { get; init; }
    public DateTime? BlockedAt { get; init; }
    public string? Notes { get; init; }
}
