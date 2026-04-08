using App.Domain.ValueObjects;
using App.Domain.Venues;
using App.DTO.v1.Venues.Admin;
using App.DTO.v1.Venues.Common;
using App.DTO.v1.Venues.Employee;
using App.DTO.v1.Venues.Membership;
using App.DTO.v1.Venues.Public;

namespace App.BLL.Mappers;

internal static class VenueDtoMapper
{
    public static MoneyDto ToDto(this Money money) => new()
    {
        Amount = money.Amount,
        Currency = money.Currency
    };

    public static CapacityProfileDto ToDto(this CapacityProfile capacity) => new()
    {
        Minimum = capacity.Minimum,
        Recommended = capacity.Recommended,
        Maximum = capacity.Maximum
    };

    public static ScheduleWindowDto ToDto(this ScheduleWindow schedule) => new()
    {
        StartsAt = schedule.StartsAt,
        EndsAt = schedule.EndsAt,
        DurationMinutes = schedule.DurationMinutes
    };

    public static PublicVenueSummaryDto ToPublicSummaryDto(this Venue venue) => new()
    {
        VenueId = venue.Id,
        Name = venue.Name,
        Slug = venue.Slug,
        City = venue.City,
        Country = venue.Country,
        AddressLine1 = venue.AddressLine1,
        Description = venue.Description,
        DefaultHourlyRate = venue.DefaultHourlyRate.ToDto(),
        Capacity = venue.CapacityProfile.ToDto(),
        SpaceCount = venue.Spaces.Count,
        UpcomingBookingsCount = venue.Bookings.Count(booking =>
            booking.Status.AppearsOnCalendar() &&
            booking.Schedule.StartsAt >= DateTime.UtcNow)
    };

    public static PublicVenueDetailDto ToPublicDetailDto(this Venue venue) => new()
    {
        VenueId = venue.Id,
        Name = venue.Name,
        Slug = venue.Slug,
        City = venue.City,
        Country = venue.Country,
        AddressLine1 = venue.AddressLine1,
        Description = venue.Description,
        DefaultHourlyRate = venue.DefaultHourlyRate.ToDto(),
        Capacity = venue.CapacityProfile.ToDto(),
        UpcomingBookingsCount = venue.Bookings.Count(booking =>
            booking.Status.AppearsOnCalendar() &&
            booking.Schedule.StartsAt >= DateTime.UtcNow),
        Spaces = venue.Spaces
            .OrderBy(space => space.Name)
            .Select(space => space.ToPublicSpaceSummaryDto())
            .ToList()
    };

    public static PublicSpaceSummaryDto ToPublicSpaceSummaryDto(this Space space) => new()
    {
        SpaceId = space.Id,
        Name = space.Name,
        Code = space.Code,
        Description = space.Description,
        Status = space.Status.ToString(),
        MinimumBookingDurationMinutes = space.MinimumBookingDurationMinutes,
        HourlyRate = space.HourlyRate.ToDto(),
        Capacity = space.CapacityProfile.ToDto(),
        Layouts = space.Layouts
            .OrderByDescending(layout => layout.IsDefault)
            .ThenBy(layout => layout.Name)
            .Select(layout => new PublicSpaceLayoutOptionDto
            {
                LayoutId = layout.Id,
                Name = layout.Name,
                LayoutType = layout.LayoutType.ToString(),
                Capacity = layout.Capacity,
                IsDefault = layout.IsDefault,
                Notes = layout.Notes
            })
            .ToList()
    };

    public static EmployeeBookingSummaryDto ToEmployeeBookingSummaryDto(this Booking booking) => new()
    {
        BookingId = booking.Id,
        Title = booking.Title,
        ClientName = booking.ClientName,
        Status = booking.Status.ToString(),
        SpaceName = booking.Space.Name,
        Schedule = booking.Schedule.ToDto(),
        ExpectedAttendees = booking.ExpectedAttendees,
        SpaceCharge = booking.SpaceCharge.ToDto(),
        CateringOrderCount = booking.CateringOrders.Count,
        EquipmentAllocationCount = booking.EquipmentAllocations.Count,
        HasCoordinationNotes = !string.IsNullOrWhiteSpace(booking.CoordinationNotes)
    };

    public static UserBookingRequestSummaryDto ToUserBookingRequestSummaryDto(this Booking booking) => new()
    {
        BookingId = booking.Id,
        VenueId = booking.VenueId,
        VenueName = booking.Venue.Name,
        VenueSlug = booking.Venue.Slug,
        SpaceName = booking.Space.Name,
        Title = booking.Title,
        ClientName = booking.ClientName,
        Status = booking.Status.ToString(),
        Schedule = booking.Schedule.ToDto(),
        ExpectedAttendees = booking.ExpectedAttendees,
        AppearsOnCalendar = booking.Status.AppearsOnCalendar()
    };

    public static UserBookingRequestDetailDto ToUserBookingRequestDetailDto(this Booking booking) => new()
    {
        BookingId = booking.Id,
        VenueId = booking.VenueId,
        VenueName = booking.Venue.Name,
        VenueSlug = booking.Venue.Slug,
        SpaceName = booking.Space.Name,
        Title = booking.Title,
        ClientName = booking.ClientName,
        Status = booking.Status.ToString(),
        Schedule = booking.Schedule.ToDto(),
        ExpectedAttendees = booking.ExpectedAttendees,
        AppearsOnCalendar = booking.Status.AppearsOnCalendar(),
        SpaceCharge = booking.SpaceCharge.ToDto(),
        CateringOrderCount = booking.CateringOrders.Count,
        EquipmentAllocationCount = booking.EquipmentAllocations.Count,
        CateringLockedAt = booking.CateringOrders
            .OrderBy(order => order.LockedAt)
            .Select(order => (DateTime?) order.LockedAt)
            .FirstOrDefault(),
        CoordinationNotes = booking.CoordinationNotes
    };

    public static CateringOrderSummaryDto ToCateringSummaryDto(this CateringOrder order) => new()
    {
        CateringOrderId = order.Id,
        BookingId = order.BookingId,
        BookingTitle = order.Booking.Title,
        ProviderName = order.ProviderName,
        Status = order.Status.ToString(),
        LockedAt = order.LockedAt,
        IsLocked = order.LockedAt <= DateTime.UtcNow,
        GuestCount = order.GuestCount,
        TotalPrice = order.TotalPrice.ToDto(),
        Notes = order.Notes,
        Lines = order.Lines
            .OrderBy(line => line.Name)
            .Select(line => new CateringOrderLineDto
            {
                LineId = line.Id,
                Name = line.Name,
                Quantity = line.Quantity,
                UnitPrice = line.UnitPrice.ToDto(),
                DietaryNotes = line.DietaryNotes
            })
            .ToList()
    };

    public static EquipmentAllocationSummaryDto ToEquipmentAllocationSummaryDto(this EquipmentAllocation allocation) => new()
    {
        AllocationId = allocation.Id,
        EquipmentName = allocation.EquipmentInventoryItem.Name,
        Status = allocation.Status.ToString(),
        Quantity = allocation.Quantity,
        SpaceName = allocation.Space?.Name ?? string.Empty,
        Schedule = allocation.Schedule.ToDto(),
        TotalPrice = allocation.TotalPrice.ToDto()
    };

    public static ActiveVenueOptionDto ToActiveVenueOptionDto(this VenueMembership membership, Guid? activeVenueId) => new()
    {
        MembershipId = membership.Id,
        VenueId = membership.VenueId,
        VenueName = membership.Venue.Name,
        CompanyName = membership.Company.Name,
        AccessLevel = membership.AccessLevel.ToString(),
        VenueStatus = membership.Venue.Status.ToString(),
        MembershipStatus = membership.Status.ToString(),
        IsCurrentVenue = activeVenueId == membership.VenueId,
        IsDefaultVenue = membership.IsDefaultVenue,
        CanSelect = !membership.Status.BlocksVenueSelection()
    };

    public static ActiveVenueSelectionResultDto ToActiveVenueSelectionResultDto(this VenueMembership membership) => new()
    {
        VenueId = membership.VenueId,
        MembershipId = membership.Id,
        VenueName = membership.Venue.Name,
        CompanyName = membership.Company.Name,
        AccessLevel = membership.AccessLevel.ToString(),
        VenueStatus = membership.Venue.Status.ToString()
    };

    public static VenueMembershipSummaryDto ToMembershipSummaryDto(this VenueMembership membership) => new()
    {
        MembershipId = membership.Id,
        UserId = membership.UserId,
        VenueId = membership.VenueId,
        CompanyId = membership.CompanyId,
        VenueName = membership.Venue.Name,
        CompanyName = membership.Company.Name,
        AccessLevel = membership.AccessLevel.ToString(),
        Status = membership.Status.ToString(),
        IsDefaultVenue = membership.IsDefaultVenue,
        GrantedAt = membership.GrantedAt,
        BlockedAt = membership.BlockedAt,
        Notes = membership.Notes
    };

    public static VenueAccessRequestListItemDto ToAccessRequestListItemDto(this VenueAccessRequest request) => new()
    {
        RequestId = request.Id,
        CompanyName = request.CompanyName,
        VenueName = request.VenueName,
        ContactName = request.ContactName,
        ContactEmail = request.ContactEmail,
        City = request.City,
        Country = request.Country,
        EstimatedMonthlyBookings = request.EstimatedMonthlyBookings,
        Status = request.Status.ToString(),
        SubmittedAt = request.SubmittedAt,
        ReviewedAt = request.ReviewedAt,
        CanAssignRights = request.Status.AllowsMembershipAssignment() &&
                          request.CompanyId.HasValue &&
                          request.VenueId.HasValue
    };

    public static VenueAccessRequestDetailDto ToAccessRequestDetailDto(this VenueAccessRequest request, VenueMembership? membership) => new()
    {
        RequestId = request.Id,
        CompanyName = request.CompanyName,
        VenueName = request.VenueName,
        ContactName = request.ContactName,
        ContactEmail = request.ContactEmail,
        City = request.City,
        Country = request.Country,
        EstimatedMonthlyBookings = request.EstimatedMonthlyBookings,
        Status = request.Status.ToString(),
        SubmittedAt = request.SubmittedAt,
        ReviewedAt = request.ReviewedAt,
        CanAssignRights = request.Status.AllowsMembershipAssignment() &&
                          request.CompanyId.HasValue &&
                          request.VenueId.HasValue,
        RequestorUserId = request.RequestorUserId,
        RequestorEmail = request.RequestorUser.Email,
        ReviewedByUserId = request.ReviewedByUserId,
        ReviewedByEmail = request.ReviewedByUser != null ? request.ReviewedByUser.Email : null,
        CompanyId = request.CompanyId,
        VenueId = request.VenueId,
        ContactPhone = request.ContactPhone,
        AddressLine1 = request.AddressLine1,
        Notes = request.Notes,
        ReviewNotes = request.ReviewNotes,
        ApprovedAccessLevel = request.ApprovedAccessLevel?.ToString(),
        AssignedMembership = membership == null
            ? null
            : new VenueMembershipAssignmentDto
            {
                MembershipId = membership.Id,
                UserId = membership.UserId,
                VenueId = membership.VenueId,
                AccessLevel = membership.AccessLevel.ToString(),
                Status = membership.Status.ToString()
            }
    };

    public static SpaceConfigurationDto ToSpaceConfigurationDto(this Space space) => new()
    {
        SpaceId = space.Id,
        Name = space.Name,
        Code = space.Code,
        Status = space.Status.ToString(),
        Description = space.Description,
        MinimumBookingDurationMinutes = space.MinimumBookingDurationMinutes,
        HourlyRate = space.HourlyRate.ToDto(),
        Capacity = space.CapacityProfile.ToDto(),
        Layouts = space.Layouts
            .OrderByDescending(layout => layout.IsDefault)
            .ThenBy(layout => layout.Name)
            .Select(layout => new SpaceLayoutDto
            {
                LayoutId = layout.Id,
                Name = layout.Name,
                LayoutType = layout.LayoutType.ToString(),
                Capacity = layout.Capacity,
                IsDefault = layout.IsDefault,
                Notes = layout.Notes
            })
            .ToList()
    };
}
