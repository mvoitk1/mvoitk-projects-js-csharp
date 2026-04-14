namespace App.DTO.v1.Products;

public class ProductVariantDto
{
    public Guid Id { get; set; }
    public string Sku { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int StockQty { get; set; }
    public bool IsActive { get; set; }

    public Guid ColorId { get; set; }
    public string ColorName { get; set; } = string.Empty;
    public string ColorHex { get; set; } = string.Empty;

    public Guid SizeId { get; set; }
    public string SizeCode { get; set; } = string.Empty;
    public string SizeDisplayName { get; set; } = string.Empty;
}
