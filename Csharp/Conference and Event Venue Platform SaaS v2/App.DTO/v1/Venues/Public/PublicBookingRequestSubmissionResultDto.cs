namespace App.DTO.v1.Venues.Public;

public class PublicBookingRequestSubmissionResultDto
{
    public Guid BookingId { get; init; }
    public string Status { get; init; } = default!;
    public Guid VenueId { get; init; }
    public Guid SpaceId { get; init; }
}
