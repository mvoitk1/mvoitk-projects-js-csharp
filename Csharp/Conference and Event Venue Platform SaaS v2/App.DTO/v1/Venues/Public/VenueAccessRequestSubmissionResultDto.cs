namespace App.DTO.v1.Venues.Public;

public class VenueAccessRequestSubmissionResultDto
{
    public Guid RequestId { get; init; }
    public string Status { get; init; } = default!;
    public DateTime SubmittedAt { get; init; }
}
