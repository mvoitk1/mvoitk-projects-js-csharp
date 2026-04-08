using App.Domain.ValueObjects;

namespace App.Domain.Venues;

public class CateringOrder : BaseEntity
{
    public Guid BookingId { get; set; }
    public Booking Booking { get; set; } = default!;

    public string ProviderName { get; set; } = default!;
    public CateringOrderStatus Status { get; set; } = CateringOrderStatus.Draft;
    public DateTime LockedAt { get; set; }
    public int GuestCount { get; set; }
    public Money TotalPrice { get; set; } = Money.Zero();
    public string? Notes { get; set; }

    public ICollection<CateringOrderLine> Lines { get; set; } = new List<CateringOrderLine>();
}
