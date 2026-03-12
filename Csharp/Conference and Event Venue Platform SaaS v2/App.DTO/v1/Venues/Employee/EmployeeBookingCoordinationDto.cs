namespace App.DTO.v1.Venues.Employee;

public class EmployeeBookingCoordinationDto
{
    public EmployeeBookingSummaryDto Booking { get; init; } = default!;
    public string? CoordinationNotes { get; init; }
    public IReadOnlyList<CateringOrderSummaryDto> CateringOrders { get; init; } = [];
    public IReadOnlyList<EquipmentAllocationSummaryDto> EquipmentAllocations { get; init; } = [];
}
