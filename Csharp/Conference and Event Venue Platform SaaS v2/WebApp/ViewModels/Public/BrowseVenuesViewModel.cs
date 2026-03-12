namespace WebApp.ViewModels.Public;

public class BrowseVenuesViewModel
{
    public IReadOnlyList<PublicVenueCardViewModel> Venues { get; init; } = [];
}
