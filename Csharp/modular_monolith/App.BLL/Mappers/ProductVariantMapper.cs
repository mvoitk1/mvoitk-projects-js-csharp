using App.Domain;
using App.BLL.DTO.Products;

namespace App.BLL.Mappers;

public static class ProductVariantMapper
{
    public static ProductVariantDto ToDto(ProductVariant e) => new()
    {
        Id = e.Id,
        Sku = e.Sku,
        Price = e.Price,
        StockQty = e.StockQty,
        IsActive = e.IsActive,
        ColorId = e.ColorId,
        ColorName = e.Color?.Name.Tr() ?? string.Empty,
        ColorHex = e.Color?.HexCode ?? string.Empty,
        SizeId = e.SizeId,
        SizeCode = e.Size?.SizeCode ?? string.Empty,
        SizeDisplayName = e.Size?.DisplayName.Tr() ?? string.Empty
    };
}
