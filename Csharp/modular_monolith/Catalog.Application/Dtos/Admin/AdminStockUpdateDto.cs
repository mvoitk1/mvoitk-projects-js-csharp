using System.ComponentModel.DataAnnotations;

namespace Catalog.Application.Dtos.Admin;

public class AdminStockUpdateDto
{
    [Range(0, 100000)]
    public int StockQty { get; set; }
}
