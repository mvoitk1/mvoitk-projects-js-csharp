namespace WebApp.ViewModels.Public;

public class PublicVenueCardViewModel
{
    public Guid VenueId { get; init; }
    public string Slug { get; init; } = default!;
    public string Name { get; init; } = default!;
    public string City { get; init; } = default!;
    public string Country { get; init; } = default!;
    public string AddressLine1 { get; init; } = default!;
    public string? Description { get; init; }
    public string HourlyRate { get; init; } = default!;
    public int Capacity { get; init; }
    public int SpaceCount { get; init; }
    public int UpcomingBookingsCount { get; init; }
}
