using System.ComponentModel.DataAnnotations;

namespace App.DTO.v1.Cart;

public class UpdateCartItemDto
{
    [Range(1, 100)]
    public int Quantity { get; set; }
}
