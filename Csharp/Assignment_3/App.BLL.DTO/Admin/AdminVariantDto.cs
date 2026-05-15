namespace App.BLL.DTO.Admin;

public class AdminVariantDto
{
    public Guid Id { get; set; }
    public string Sku { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal UnitPrice { get; set; }
    public int StockQty { get; set; }
    public bool IsActive { get; set; }
    public Guid ColorId { get; set; }
    public string ColorName { get; set; } = string.Empty;
    public Guid SizeId { get; set; }
    public string SizeCode { get; set; } = string.Empty;
}
