namespace App.DTO.v1.Venues.Admin;

public class VenueAccessRequestListItemDto
{
    public Guid RequestId { get; init; }
    public string CompanyName { get; init; } = default!;
    public string VenueName { get; init; } = default!;
    public string ContactName { get; init; } = default!;
    public string ContactEmail { get; init; } = default!;
    public string City { get; init; } = default!;
    public string Country { get; init; } = default!;
    public int EstimatedMonthlyBookings { get; init; }
    public string Status { get; init; } = default!;
    public DateTime SubmittedAt { get; init; }
    public DateTime? ReviewedAt { get; init; }
    public bool CanAssignRights { get; init; }
}
