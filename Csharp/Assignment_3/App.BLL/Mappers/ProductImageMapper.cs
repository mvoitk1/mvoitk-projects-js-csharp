using App.Domain;
using App.DTO.v1.Products;

namespace App.BLL.Mappers;

public static class ProductImageMapper
{
    public static ProductImageDto ToDto(ProductImage e) => new()
    {
        Id = e.Id,
        Url = e.Url,
        AltText = e.AltText.Tr(),
        SortOrder = e.SortOrder
    };
}
