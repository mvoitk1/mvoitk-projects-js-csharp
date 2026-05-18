using Catalog.Domain;
using Modules.SharedKernel;
using Catalog.Application.Dtos.Products;

namespace Catalog.Application.Mappers;

public static class ProductMapper
{
    public static ProductDto ToDto(Product e) => new()
    {
        Id = e.Id,
        Name = e.Name.Tr(),
        Description = e.Description.Tr(),
        Material = e.Material.Tr(),
        Gender = e.Gender.ToString(),
        IsActive = e.IsActive,
        CreatedAt = e.CreatedAt,
        CollectionId = e.CollectionId,
        CollectionName = e.Collection?.Name.Tr(),
        Images = e.Images?
            .OrderBy(i => i.SortOrder)
            .Select(ProductImageMapper.ToDto)
            .ToList() ?? [],
        Variants = e.Variants?
            .Where(v => v.IsActive)
            .Select(ProductVariantMapper.ToDto)
            .ToList() ?? [],
        CategoryNames = e.ProductCategories?
            .Select(pc => pc.Category?.Name.Tr() ?? string.Empty)
            .ToList() ?? []
    };

    public static ProductListItemDto ToListItem(Product e) => new()
    {
        Id = e.Id,
        Name = e.Name.Tr(),
        Gender = e.Gender.ToString(),
        IsActive = e.IsActive,
        LowestPrice = e.Variants?.Where(v => v.IsActive).Min(v => (decimal?)v.Price),
        PrimaryImageUrl = e.Images?.OrderBy(i => i.SortOrder).FirstOrDefault()?.Url,
        CollectionName = e.Collection?.Name.Tr()
    };
}
