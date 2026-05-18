using System.ComponentModel.DataAnnotations;

namespace Sales.Web.Dtos.v1.Cart;

public class UpdateCartItemDto
{
    [Range(1, 100)]
    public int Quantity { get; set; }
}
