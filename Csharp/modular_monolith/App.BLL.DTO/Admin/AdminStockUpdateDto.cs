using System.ComponentModel.DataAnnotations;

namespace App.BLL.DTO.Admin;

public class AdminStockUpdateDto
{
    [Range(0, 100000)]
    public int StockQty { get; set; }
}
