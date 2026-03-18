namespace App.DTO.v1.Venues.Public;

public class SubmitPublicBookingRequestDto
{
    public Guid SpaceId { get; init; }
    public Guid? LayoutId { get; init; }
    public string EventTitle { get; init; } = default!;
    public string ClientName { get; init; } = default!;
    public DateTime StartsAt { get; init; }
    public DateTime EndsAt { get; init; }
    public int ExpectedAttendees { get; init; }
    public string? CateringNotes { get; init; }
    public string? SetupRequirements { get; init; }
    public string? AdditionalRequirements { get; init; }
}
