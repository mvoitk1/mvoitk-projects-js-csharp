using System.ComponentModel.DataAnnotations;

namespace Sales.Web.Dtos.v1.Cart;

public class AddToCartDto
{
    [Required]
    public Guid ProductVariantId { get; set; }

    [Range(1, 100)]
    public int Quantity { get; set; } = 1;
}
