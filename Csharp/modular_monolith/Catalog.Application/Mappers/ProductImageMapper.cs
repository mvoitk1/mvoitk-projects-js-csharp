using Catalog.Domain;
using Modules.SharedKernel;
using Catalog.Application.Dtos.Products;

namespace Catalog.Application.Mappers;

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
