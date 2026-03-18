namespace App.DTO.v1.Venues.Employee;

public class EmployeeWorkspaceDashboardDto
{
    public string VenueName { get; init; } = default!;
    public int UpcomingBookingsCount { get; init; }
    public int DraftOrPendingBookingsCount { get; init; }
    public int CateringOrdersCount { get; init; }
    public int LockedCateringOrdersCount { get; init; }
    public int EquipmentAllocationsCount { get; init; }
    public IReadOnlyList<EmployeeBookingSummaryDto> UpcomingBookings { get; init; } = [];
    public IReadOnlyList<EmployeeBookingSummaryDto> PendingApprovalBookings { get; init; } = [];
}
