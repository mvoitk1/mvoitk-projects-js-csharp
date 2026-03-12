using System.Globalization;
using App.DTO.v1.Venues.Public;
using App.Resources.Views.Public;

namespace WebApp.ViewModels.Public;

public static class PublicVenueViewModelMapper
{
    public static PublicLandingPageViewModel ToViewModel(this PublicLandingPageDto dto) =>
        new()
        {
            ActiveVenueCount = dto.ActiveVenueCount,
            CityCount = dto.CityCount,
            UpcomingConfirmedBookingCount = dto.UpcomingConfirmedBookingCount,
            FeaturedVenues = dto.FeaturedVenues.Select(ToViewModel).ToList()
        };

    public static BrowseVenuesViewModel ToBrowseViewModel(this IReadOnlyList<PublicVenueSummaryDto> venues) =>
        new()
        {
            Venues = venues.Select(ToViewModel).ToList()
        };

    public static PublicVenueCardViewModel ToViewModel(this PublicVenueSummaryDto dto)
    {
        var culture = CultureInfo.CurrentCulture;
        var amount = dto.DefaultHourlyRate.Amount.ToString("0.##", culture);

        return new PublicVenueCardViewModel
        {
            VenueId = dto.VenueId,
            Name = dto.Name,
            City = dto.City,
            Country = dto.Country,
            AddressLine1 = dto.AddressLine1,
            Description = dto.Description,
            HourlyRate = string.Format(
                culture,
                Pages.VenueFromRate,
                $"{amount} {dto.DefaultHourlyRate.Currency}",
                Pages.VenueHourlyUnit),
            Capacity = dto.Capacity.Maximum,
            SpaceCount = dto.SpaceCount,
            UpcomingBookingsCount = dto.UpcomingBookingsCount
        };
    }
}
