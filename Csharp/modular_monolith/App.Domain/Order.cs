using Base.Domain;
using App.Domain.Enums;

namespace App.Domain;

public class Order : DomainEntityId
{
    public string OrderNumber { get; set; } = string.Empty;
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Shipping address snapshot (flat fields — no Address entity in Phase 1)
    public string ShippingFirstName { get; set; } = string.Empty;
    public string ShippingLastName { get; set; } = string.Empty;
    public string ShippingEmail { get; set; } = string.Empty;
    public string ShippingPhone { get; set; } = string.Empty;
    public string ShippingCountry { get; set; } = string.Empty;
    public string ShippingCity { get; set; } = string.Empty;
    public string ShippingStreet { get; set; } = string.Empty;
    public string ShippingPostalCode { get; set; } = string.Empty;

    // Owned by Users module — stored as plain Guid (no FK constraint across modules).
    public Guid AppUserId { get; set; }

    public ICollection<OrderItem> Items { get; set; } = null!;
}
