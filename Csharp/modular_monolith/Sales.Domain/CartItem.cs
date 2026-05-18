using Base.Domain;

namespace Sales.Domain;

public class CartItem : DomainEntityId
{
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }

    public Guid CartId { get; set; }
    public Cart? Cart { get; set; }

    // Owned by Catalog module — plain Guid only. Variant details (price, stock)
    // are fetched via MediatR (GetVariantPricingQuery) when needed.
    public Guid ProductVariantId { get; set; }
}
