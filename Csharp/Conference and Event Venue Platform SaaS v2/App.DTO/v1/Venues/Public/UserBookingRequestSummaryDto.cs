using App.DTO.v1.Venues.Common;

namespace App.DTO.v1.Venues.Public;

public class UserBookingRequestSummaryDto
{
    public Guid BookingId { get; init; }
    public Guid VenueId { get; init; }
    public string VenueName { get; init; } = default!;
    public string VenueSlug { get; init; } = default!;
    public string SpaceName { get; init; } = default!;
    public string Title { get; init; } = default!;
    public string ClientName { get; init; } = default!;
    public string Status { get; init; } = default!;
    public ScheduleWindowDto Schedule { get; init; } = default!;
    public int ExpectedAttendees { get; init; }
    public bool AppearsOnCalendar { get; init; }
}
