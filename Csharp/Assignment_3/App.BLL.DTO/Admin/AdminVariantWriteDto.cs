using System.ComponentModel.DataAnnotations;

namespace App.BLL.DTO.Admin;

public class AdminVariantWriteDto
{
    [Required]
    public string Sku { get; set; } = string.Empty;

    [Range(0, 99999.99)]
    public decimal Price { get; set; }

    [Range(0, 99999.99)]
    public decimal UnitPrice { get; set; }

    [Range(0, 100000)]
    public int StockQty { get; set; }

    public bool IsActive { get; set; } = true;

    [Required]
    public Guid ColorId { get; set; }

    [Required]
    public Guid SizeId { get; set; }
}
