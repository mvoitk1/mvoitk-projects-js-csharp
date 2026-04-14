using App.DTO.v1.Venues.Common;

namespace App.DTO.v1.Venues.Employee;

public class EquipmentAllocationSummaryDto
{
    public Guid AllocationId { get; init; }
    public string EquipmentName { get; init; } = default!;
    public string Status { get; init; } = default!;
    public int Quantity { get; init; }
    public string SpaceName { get; init; } = default!;
    public ScheduleWindowDto Schedule { get; init; } = default!;
    public MoneyDto TotalPrice { get; init; } = default!;
}
