using App.Domain;
using App.DTO.v1.Cart;

namespace App.BLL.Mappers;

public static class CartMapper
{
    public static CartDto ToDto(Cart e) => new()
    {
        Id = e.Id,
        Status = e.Status.ToString(),
        Items = e.Items?.Select(ToItemDto).ToList() ?? []
    };

    public static CartItemDto ToItemDto(CartItem i) => new()
    {
        Id = i.Id,
        Quantity = i.Quantity,
        UnitPrice = i.UnitPrice,
        ProductVariantId = i.ProductVariantId,
        ProductName = i.ProductVariant?.Product?.Name.Tr() ?? string.Empty,
        Sku = i.ProductVariant?.Sku ?? string.Empty,
        ColorName = i.ProductVariant?.Color?.Name.Tr() ?? string.Empty,
        SizeCode = i.ProductVariant?.Size?.SizeCode ?? string.Empty,
        ImageUrl = i.ProductVariant?.Product?.Images?
            .OrderBy(img => img.SortOrder).FirstOrDefault()?.Url
    };
}
