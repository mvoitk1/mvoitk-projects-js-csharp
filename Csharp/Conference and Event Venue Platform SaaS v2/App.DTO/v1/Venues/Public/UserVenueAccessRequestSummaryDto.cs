namespace App.DTO.v1.Venues.Public;

public class UserVenueAccessRequestSummaryDto
{
    public Guid RequestId { get; init; }
    public string CompanyName { get; init; } = default!;
    public string VenueName { get; init; } = default!;
    public string Status { get; init; } = default!;
    public DateTime SubmittedAt { get; init; }
    public DateTime? ReviewedAt { get; init; }
    public string? ApprovedAccessLevel { get; init; }
    public bool HasAssignedMembership { get; init; }
}
