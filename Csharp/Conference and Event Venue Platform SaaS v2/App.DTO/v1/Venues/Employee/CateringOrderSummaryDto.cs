using App.DTO.v1.Venues.Common;

namespace App.DTO.v1.Venues.Employee;

public class CateringOrderSummaryDto
{
    public Guid CateringOrderId { get; init; }
    public Guid BookingId { get; init; }
    public string BookingTitle { get; init; } = default!;
    public string ProviderName { get; init; } = default!;
    public string Status { get; init; } = default!;
    public DateTime LockedAt { get; init; }
    public bool IsLocked { get; init; }
    public int GuestCount { get; init; }
    public MoneyDto TotalPrice { get; init; } = default!;
    public string? Notes { get; init; }
    public IReadOnlyList<CateringOrderLineDto> Lines { get; init; } = [];
}
