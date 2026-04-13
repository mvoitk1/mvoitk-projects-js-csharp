using System.ComponentModel.DataAnnotations;

namespace App.DTO.v1.Admin;

public class AdminStockUpdateDto
{
    [Range(0, 100000)]
    public int StockQty { get; set; }
}
