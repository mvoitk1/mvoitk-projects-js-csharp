using App.DTO.v1.Venues.Employee;

namespace App.BLL.Services;

public interface IEmployeeWorkspaceService
{
    Task<EmployeeWorkspaceDashboardDto> GetDashboardAsync(Guid userId, Guid venueId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EmployeeBookingSummaryDto>> GetBookingsAsync(Guid userId, Guid venueId, CancellationToken cancellationToken = default);
    Task<EmployeeBookingCoordinationDto> GetCoordinationAsync(Guid userId, Guid venueId, Guid bookingId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CateringOrderSummaryDto>> GetCateringOrdersAsync(Guid userId, Guid venueId, CancellationToken cancellationToken = default);
    Task<BookingApprovalResultDto> ApproveBookingRequestAsync(
        Guid userId,
        Guid venueId,
        Guid bookingId,
        CancellationToken cancellationToken = default);
    Task<CateringOrderSummaryDto> UpdateCateringOrderAsync(
        Guid userId,
        Guid venueId,
        Guid cateringOrderId,
        CateringOrderEditDto dto,
        CancellationToken cancellationToken = default);
}
