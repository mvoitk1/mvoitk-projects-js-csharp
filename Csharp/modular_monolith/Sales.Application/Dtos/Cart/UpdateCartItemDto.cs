using System.ComponentModel.DataAnnotations;

namespace Sales.Application.Dtos.Cart;

public class UpdateCartItemDto
{
    [Range(1, 100)]
    public int Quantity { get; set; }
}
