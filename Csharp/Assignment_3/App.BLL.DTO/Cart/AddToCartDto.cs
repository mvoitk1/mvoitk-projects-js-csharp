using System.ComponentModel.DataAnnotations;

namespace App.BLL.DTO.Cart;

public class AddToCartDto
{
    [Required]
    public Guid ProductVariantId { get; set; }

    [Range(1, 100)]
    public int Quantity { get; set; } = 1;
}
