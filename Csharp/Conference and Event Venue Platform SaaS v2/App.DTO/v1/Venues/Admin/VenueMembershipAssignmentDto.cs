namespace App.DTO.v1.Venues.Admin;

public class VenueMembershipAssignmentDto
{
    public Guid MembershipId { get; init; }
    public Guid UserId { get; init; }
    public Guid VenueId { get; init; }
    public string AccessLevel { get; init; } = default!;
    public string Status { get; init; } = default!;
}
