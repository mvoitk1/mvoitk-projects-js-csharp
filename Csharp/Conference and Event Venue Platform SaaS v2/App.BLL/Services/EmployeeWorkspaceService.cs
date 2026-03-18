using App.BLL.Mappers;
using App.DAL.EF;
using App.Domain.ValueObjects;
using App.Domain.Venues;
using App.DTO.v1.Venues.Employee;
using Microsoft.EntityFrameworkCore;

namespace App.BLL.Services;

public class EmployeeWorkspaceService(AppDbContext context) : IEmployeeWorkspaceService
{
    public async Task<EmployeeWorkspaceDashboardDto> GetDashboardAsync(
        Guid userId,
        Guid venueId,
        CancellationToken cancellationToken = default)
    {
        await VenueAccessValidation.RequireOperationalMembershipAsync(context, userId, venueId, cancellationToken);

        var venue = await context.Venues
            .AsNoTracking()
            .SingleAsync(item => item.Id == venueId, cancellationToken);

        var bookings = await GetVenueBookingsQuery(venueId)
            .Where(booking => booking.Schedule.StartsAt >= DateTime.UtcNow.AddDays(-1))
            .OrderBy(booking => booking.Schedule.StartsAt)
            .ToListAsync(cancellationToken);

        var cateringOrders = bookings.SelectMany(booking => booking.CateringOrders).ToList();
        var calendarBookings = bookings
            .Where(booking => booking.Status.AppearsOnCalendar())
            .ToList();

        return new EmployeeWorkspaceDashboardDto
        {
            VenueName = venue.Name,
            UpcomingBookingsCount = calendarBookings.Count(booking => booking.Schedule.StartsAt >= DateTime.UtcNow),
            DraftOrPendingBookingsCount = bookings.Count(booking => booking.Status is BookingStatus.Draft or BookingStatus.PendingApproval),
            CateringOrdersCount = cateringOrders.Count,
            LockedCateringOrdersCount = cateringOrders.Count(order => order.LockedAt <= DateTime.UtcNow),
            EquipmentAllocationsCount = bookings.SelectMany(booking => booking.EquipmentAllocations).Count(),
            UpcomingBookings = calendarBookings.Take(5).Select(booking => booking.ToEmployeeBookingSummaryDto()).ToList(),
            PendingApprovalBookings = bookings
                .Where(booking => booking.Status == BookingStatus.PendingApproval)
                .OrderBy(booking => booking.Schedule.StartsAt)
                .Take(5)
                .Select(booking => booking.ToEmployeeBookingSummaryDto())
                .ToList()
        };
    }

    public async Task<IReadOnlyList<EmployeeBookingSummaryDto>> GetBookingsAsync(
        Guid userId,
        Guid venueId,
        CancellationToken cancellationToken = default)
    {
        await VenueAccessValidation.RequireOperationalMembershipAsync(context, userId, venueId, cancellationToken);

        var bookings = await GetVenueBookingsQuery(venueId)
            .OrderBy(booking => booking.Schedule.StartsAt)
            .ToListAsync(cancellationToken);

        return bookings.Select(booking => booking.ToEmployeeBookingSummaryDto()).ToList();
    }

    public async Task<EmployeeBookingCoordinationDto> GetCoordinationAsync(
        Guid userId,
        Guid venueId,
        Guid bookingId,
        CancellationToken cancellationToken = default)
    {
        await VenueAccessValidation.RequireOperationalMembershipAsync(context, userId, venueId, cancellationToken);

        var booking = await GetVenueBookingsQuery(venueId)
            .SingleOrDefaultAsync(item => item.Id == bookingId, cancellationToken);

        if (booking == null)
        {
            throw new KeyNotFoundException("Booking was not found.");
        }

        return new EmployeeBookingCoordinationDto
        {
            Booking = booking.ToEmployeeBookingSummaryDto(),
            CoordinationNotes = booking.CoordinationNotes,
            CateringOrders = booking.CateringOrders.Select(order => order.ToCateringSummaryDto()).ToList(),
            EquipmentAllocations = booking.EquipmentAllocations
                .OrderBy(allocation => allocation.Schedule.StartsAt)
                .Select(allocation => allocation.ToEquipmentAllocationSummaryDto())
                .ToList()
        };
    }

    public async Task<IReadOnlyList<CateringOrderSummaryDto>> GetCateringOrdersAsync(
        Guid userId,
        Guid venueId,
        CancellationToken cancellationToken = default)
    {
        await VenueAccessValidation.RequireOperationalMembershipAsync(context, userId, venueId, cancellationToken);

        var orders = await context.CateringOrders
            .AsNoTracking()
            .Where(order => order.Booking.VenueId == venueId)
            .Include(order => order.Booking)
            .Include(order => order.Lines)
            .OrderBy(order => order.Booking.Schedule.StartsAt)
            .ToListAsync(cancellationToken);

        return orders.Select(order => order.ToCateringSummaryDto()).ToList();
    }

    public async Task<BookingApprovalResultDto> ApproveBookingRequestAsync(
        Guid userId,
        Guid venueId,
        Guid bookingId,
        CancellationToken cancellationToken = default)
    {
        await VenueAccessValidation.RequireOperationalMembershipAsync(context, userId, venueId, cancellationToken);

        var booking = await context.Bookings
            .AsTracking()
            .Include(item => item.Space)
            .SingleOrDefaultAsync(
                item => item.Id == bookingId &&
                        item.VenueId == venueId,
                cancellationToken);

        if (booking == null)
        {
            throw new KeyNotFoundException("Booking was not found.");
        }

        if (booking.Status != BookingStatus.PendingApproval)
        {
            throw new InvalidOperationException("Only pending booking requests can be approved.");
        }

        var hasConflict = await context.Bookings
            .AsNoTracking()
            .Where(item => item.Id != booking.Id &&
                           item.SpaceId == booking.SpaceId &&
                           item.Status == BookingStatus.Confirmed)
            .AnyAsync(
                item => item.Schedule.StartsAt < booking.Schedule.EndsAt &&
                        booking.Schedule.StartsAt < item.Schedule.EndsAt,
                cancellationToken);

        if (hasConflict)
        {
            throw new InvalidOperationException("This space is no longer available for the requested time.");
        }

        booking.Status = BookingStatus.Confirmed;
        await context.SaveChangesAsync(cancellationToken);

        return new BookingApprovalResultDto
        {
            BookingId = booking.Id,
            Status = booking.Status.ToString(),
            ApprovedAt = DateTime.UtcNow
        };
    }

    public async Task<CateringOrderSummaryDto> UpdateCateringOrderAsync(
        Guid userId,
        Guid venueId,
        Guid cateringOrderId,
        CateringOrderEditDto dto,
        CancellationToken cancellationToken = default)
    {
        await VenueAccessValidation.RequireOperationalMembershipAsync(context, userId, venueId, cancellationToken);

        var order = await context.CateringOrders
            .Include(item => item.Booking)
            .Include(item => item.Lines)
            .SingleOrDefaultAsync(
                item => item.Id == cateringOrderId &&
                        item.Booking.VenueId == venueId,
                cancellationToken);

        if (order == null)
        {
            throw new KeyNotFoundException("Catering order was not found.");
        }

        if (order.LockedAt <= DateTime.UtcNow)
        {
            throw new InvalidOperationException("Catering orders cannot be edited after the lock deadline.");
        }

        if (!order.Booking.Status.IsEditable())
        {
            throw new InvalidOperationException("Catering cannot be edited for bookings in the current state.");
        }

        if (dto.GuestCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(dto.GuestCount));
        }

        order.GuestCount = dto.GuestCount;
        order.Notes = string.IsNullOrWhiteSpace(dto.Notes) ? null : dto.Notes.Trim();

        var existingLines = order.Lines.ToDictionary(line => line.Id);
        order.Lines.Clear();

        foreach (var lineDto in dto.Lines)
        {
            if (string.IsNullOrWhiteSpace(lineDto.Name) || lineDto.Quantity <= 0)
            {
                throw new ArgumentException("Each catering line requires a name and positive quantity.");
            }

            var line = lineDto.LineId.HasValue && existingLines.TryGetValue(lineDto.LineId.Value, out var existingLine)
                ? existingLine
                : new CateringOrderLine();

            line.Name = lineDto.Name.Trim();
            line.Quantity = lineDto.Quantity;
            line.UnitPrice = new Money(lineDto.UnitPriceAmount, lineDto.Currency);
            line.DietaryNotes = string.IsNullOrWhiteSpace(lineDto.DietaryNotes) ? null : lineDto.DietaryNotes.Trim();

            order.Lines.Add(line);
        }

        order.TotalPrice = new Money(
            order.Lines.Sum(line => line.Quantity * line.UnitPrice.Amount),
            order.Lines.FirstOrDefault()?.UnitPrice.Currency ?? "EUR");

        await context.SaveChangesAsync(cancellationToken);

        order = await context.CateringOrders
            .AsNoTracking()
            .Include(item => item.Booking)
            .Include(item => item.Lines)
            .SingleAsync(item => item.Id == cateringOrderId, cancellationToken);

        return order.ToCateringSummaryDto();
    }

    private IQueryable<Booking> GetVenueBookingsQuery(Guid venueId) =>
        context.Bookings
            .AsNoTracking()
            .Where(booking => booking.VenueId == venueId)
            .Include(booking => booking.Space)
            .Include(booking => booking.CateringOrders)
                .ThenInclude(order => order.Lines)
            .Include(booking => booking.EquipmentAllocations)
                .ThenInclude(allocation => allocation.EquipmentInventoryItem)
            .Include(booking => booking.EquipmentAllocations)
                .ThenInclude(allocation => allocation.Space);
}
