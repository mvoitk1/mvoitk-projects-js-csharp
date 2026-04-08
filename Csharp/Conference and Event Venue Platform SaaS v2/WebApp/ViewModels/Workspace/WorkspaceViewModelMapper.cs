using App.DTO.v1.Venues.Admin;
using App.DTO.v1.Venues.Employee;
using App.DTO.v1.Venues.Public;
using App.Resources.Views.Workspace;

namespace WebApp.ViewModels.Workspace;

public static class WorkspaceViewModelMapper
{
    public static BookingCalendarItemViewModel ToVenueCalendarItemViewModel(this EmployeeBookingSummaryDto dto) =>
        new()
        {
            BookingId = dto.BookingId,
            Title = dto.Title,
            PrimaryLabel = dto.SpaceName,
            SecondaryLabel = dto.ClientName,
            Status = dto.Status,
            StartsAt = dto.Schedule.StartsAt,
            EndsAt = dto.Schedule.EndsAt,
            ExpectedAttendees = dto.ExpectedAttendees,
            LinkArea = "Employee",
            LinkController = "Coordination",
            LinkAction = "Index",
            LinkText = Pages.NavEmployeeCoordination
        };

    public static BookingCalendarItemViewModel ToUserCalendarItemViewModel(this UserBookingRequestSummaryDto dto) =>
        new()
        {
            BookingId = dto.BookingId,
            Title = dto.Title,
            PrimaryLabel = dto.VenueName,
            SecondaryLabel = dto.SpaceName,
            Status = dto.Status,
            StartsAt = dto.Schedule.StartsAt,
            EndsAt = dto.Schedule.EndsAt,
            ExpectedAttendees = dto.ExpectedAttendees,
            LinkController = "Workspace",
            LinkAction = "Booking",
            LinkText = Pages.WorkspaceBookingRequestViewDetails
        };

    public static CateringOrderEditViewModel ToEditViewModel(this CateringOrderSummaryDto dto) =>
        new()
        {
            CateringOrderId = dto.CateringOrderId,
            GuestCount = dto.GuestCount,
            Notes = dto.Notes,
            Lines = dto.Lines
                .Select(line => new CateringOrderLineEditViewModel
                {
                    LineId = line.LineId,
                    Name = line.Name,
                    Quantity = line.Quantity,
                    UnitPriceAmount = line.UnitPrice.Amount,
                    Currency = line.UnitPrice.Currency,
                    DietaryNotes = line.DietaryNotes
                })
                .ToList()
        };

    public static SpaceConfigurationFormViewModel ToFormViewModel(this SpaceConfigurationDto dto)
    {
        var layouts = dto.Layouts
            .Select(layout => new SpaceLayoutFormViewModel
            {
                LayoutId = layout.LayoutId,
                Name = layout.Name,
                LayoutType = layout.LayoutType,
                Capacity = layout.Capacity,
                IsDefault = layout.IsDefault,
                Notes = layout.Notes
            })
            .ToList();

        layouts.Add(new SpaceLayoutFormViewModel());

        return new SpaceConfigurationFormViewModel
        {
            SpaceId = dto.SpaceId,
            Name = dto.Name,
            Code = dto.Code,
            Status = dto.Status,
            Description = dto.Description,
            MinimumBookingDurationMinutes = dto.MinimumBookingDurationMinutes,
            HourlyRateAmount = dto.HourlyRate.Amount,
            Currency = dto.HourlyRate.Currency,
            MinimumCapacity = dto.Capacity.Minimum,
            RecommendedCapacity = dto.Capacity.Recommended,
            MaximumCapacity = dto.Capacity.Maximum,
            Layouts = layouts
        };
    }

    public static ReviewVenueAccessRequestViewModel ToReviewViewModel(this VenueAccessRequestDetailDto dto) =>
        new()
        {
            RequestId = dto.RequestId,
            Status = dto.Status,
            ReviewNotes = dto.ReviewNotes,
            ApprovedAccessLevel = dto.ApprovedAccessLevel,
            AssignMembership = dto.CanAssignRights && dto.AssignedMembership == null
        };
}
