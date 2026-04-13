namespace App.Domain;

public class ProductVariant : BaseEntity
{
    public string Sku { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal UnitPrice { get; set; }
    public int StockQty { get; set; } = 0;
    public bool IsActive { get; set; } = true;

    public Guid ColorId { get; set; }
    public Color? Color { get; set; }

    public Guid SizeId { get; set; }
    public Size? Size { get; set; }

    public Guid ProductId { get; set; }
    public Product? Product { get; set; }

    public ICollection<CartItem> CartItems { get; set; } = null!;
    public ICollection<OrderItem> OrderItems { get; set; } = null!;
}
