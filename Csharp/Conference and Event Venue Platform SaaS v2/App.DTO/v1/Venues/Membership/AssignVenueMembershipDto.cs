namespace App.DTO.v1.Venues.Membership;

public class AssignVenueMembershipDto
{
    public Guid UserId { get; init; }
    public Guid CompanyId { get; init; }
    public Guid VenueId { get; init; }
    public string AccessLevel { get; init; } = default!;
    public bool IsDefaultVenue { get; init; }
    public string? Notes { get; init; }
    public Guid? SourceRequestId { get; init; }
}
