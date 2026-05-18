using Base.Domain;
namespace App.Domain;

public class CartItem : DomainEntityId
{
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }

    public Guid CartId { get; set; }
    public Cart? Cart { get; set; }

    public Guid ProductVariantId { get; set; }
    public ProductVariant? ProductVariant { get; set; }
}
