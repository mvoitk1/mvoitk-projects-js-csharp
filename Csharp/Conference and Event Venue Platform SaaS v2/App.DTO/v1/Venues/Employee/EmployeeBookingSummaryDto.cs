using App.DTO.v1.Venues.Common;

namespace App.DTO.v1.Venues.Employee;

public class EmployeeBookingSummaryDto
{
    public Guid BookingId { get; init; }
    public string Title { get; init; } = default!;
    public string ClientName { get; init; } = default!;
    public string Status { get; init; } = default!;
    public string SpaceName { get; init; } = default!;
    public ScheduleWindowDto Schedule { get; init; } = default!;
    public int ExpectedAttendees { get; init; }
    public MoneyDto SpaceCharge { get; init; } = default!;
    public int CateringOrderCount { get; init; }
    public int EquipmentAllocationCount { get; init; }
    public bool HasCoordinationNotes { get; init; }
}
