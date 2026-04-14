namespace WebApp.ViewModels.Public;

public class BrowseVenuesViewModel
{
    public string? City { get; init; }
    public int? MinimumCapacity { get; init; }
    public bool HasActiveFilters => !string.IsNullOrWhiteSpace(City) || MinimumCapacity.HasValue;
    public IReadOnlyList<string> AvailableCities { get; init; } = [];
    public IReadOnlyList<PublicVenueCardViewModel> Venues { get; init; } = [];
}
