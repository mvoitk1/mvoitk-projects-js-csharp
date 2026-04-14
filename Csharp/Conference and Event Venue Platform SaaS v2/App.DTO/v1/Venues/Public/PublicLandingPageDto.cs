namespace App.DTO.v1.Venues.Public;

public class PublicLandingPageDto
{
    public int ActiveVenueCount { get; init; }
    public int CityCount { get; init; }
    public int UpcomingConfirmedBookingCount { get; init; }
    public IReadOnlyList<PublicVenueSummaryDto> FeaturedVenues { get; init; } = [];
}
