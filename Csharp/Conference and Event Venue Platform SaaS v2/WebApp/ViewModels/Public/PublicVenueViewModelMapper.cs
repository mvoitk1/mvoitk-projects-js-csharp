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

    public static BrowseVenuesViewModel ToBrowseViewModel(this BrowseVenuesResultDto dto) =>
        new()
        {
            City = dto.Filters.City,
            MinimumCapacity = dto.Filters.MinimumCapacity,
            AvailableCities = dto.AvailableCities,
            Venues = dto.Venues.Select(ToViewModel).ToList()
        };

    public static PublicVenueCardViewModel ToViewModel(this PublicVenueSummaryDto dto)
    {
        var culture = CultureInfo.CurrentCulture;
        var amount = dto.DefaultHourlyRate.Amount.ToString("0.##", culture);

        return new PublicVenueCardViewModel
        {
            VenueId = dto.VenueId,
            Slug = dto.Slug,
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

    public static PublicVenueDetailsViewModel ToDetailsViewModel(this PublicVenueDetailDto dto)
    {
        var culture = CultureInfo.CurrentCulture;
        var amount = dto.DefaultHourlyRate.Amount.ToString("0.##", culture);

        return new PublicVenueDetailsViewModel
        {
            VenueId = dto.VenueId,
            Slug = dto.Slug,
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
            UpcomingBookingsCount = dto.UpcomingBookingsCount,
            Spaces = dto.Spaces.Select(space => new PublicSpaceCardViewModel
            {
                SpaceId = space.SpaceId,
                Name = space.Name,
                Code = space.Code,
                Description = space.Description,
                Status = space.Status,
                HourlyRate = string.Format(
                    culture,
                    Pages.VenueFromRate,
                    $"{space.HourlyRate.Amount.ToString("0.##", culture)} {space.HourlyRate.Currency}",
                    Pages.VenueHourlyUnit),
                MaximumCapacity = space.Capacity.Maximum,
                MinimumBookingDurationMinutes = space.MinimumBookingDurationMinutes,
                Layouts = space.Layouts.Select(layout => new PublicSpaceLayoutCardViewModel
                {
                    LayoutId = layout.LayoutId,
                    Name = layout.Name,
                    LayoutType = layout.LayoutType,
                    Capacity = layout.Capacity,
                    IsDefault = layout.IsDefault,
                    Notes = layout.Notes
                }).ToList()
            }).ToList()
        };
    }
}
