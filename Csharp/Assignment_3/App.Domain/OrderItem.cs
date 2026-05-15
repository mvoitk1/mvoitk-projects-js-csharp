using Base.Domain;
namespace App.Domain;

public class OrderItem : DomainEntityId
{
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }

    public Guid OrderId { get; set; }
    public Order? Order { get; set; }

    public Guid ProductVariantId { get; set; }
    public ProductVariant? ProductVariant { get; set; }
}
