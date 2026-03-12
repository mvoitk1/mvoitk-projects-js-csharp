namespace App.DTO.v1.Venues.Admin;

public class ReviewVenueAccessRequestDto
{
    public Guid RequestId { get; init; }
    public string Status { get; init; } = default!;
    public string? ReviewNotes { get; init; }
    public string? ApprovedAccessLevel { get; init; }
    public bool AssignMembership { get; init; }
}
