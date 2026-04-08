namespace App.DTO.v1.Venues.Public;

public class BrowseVenuesResultDto
{
    public IReadOnlyList<PublicVenueSummaryDto> Venues { get; init; } = [];
    public IReadOnlyList<string> AvailableCities { get; init; } = [];
    public BrowseVenuesFilterDto Filters { get; init; } = new();
}
