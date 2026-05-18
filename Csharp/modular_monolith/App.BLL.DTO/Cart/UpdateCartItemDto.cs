using System.ComponentModel.DataAnnotations;

namespace App.BLL.DTO.Cart;

public class UpdateCartItemDto
{
    [Range(1, 100)]
    public int Quantity { get; set; }
}
