namespace WebApp.ViewModels.Public;

public class PublicLandingPageViewModel
{
    public int ActiveVenueCount { get; init; }
    public int CityCount { get; init; }
    public int UpcomingConfirmedBookingCount { get; init; }
    public IReadOnlyList<PublicVenueCardViewModel> FeaturedVenues { get; init; } = [];
}
