using Base.Domain;

namespace Sales.Domain;

public class OrderItem : DomainEntityId
{
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }

    public Guid OrderId { get; set; }
    public Order? Order { get; set; }

    // Owned by Catalog module — plain Guid only. Variant snapshot data is
    // copied onto the OrderItem at checkout time (UnitPrice/LineTotal).
    public Guid ProductVariantId { get; set; }

    // Snapshots from Catalog at order placement time (denormalized so the order
    // remains valid even if variants are renamed/deactivated later).
    public string ProductVariantSku { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
}
