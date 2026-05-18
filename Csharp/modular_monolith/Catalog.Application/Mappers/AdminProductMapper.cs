using Catalog.Domain;
using Modules.SharedKernel;
using Catalog.Domain.Enums;
using Catalog.Application.Dtos.Admin;

namespace Catalog.Application.Mappers;

public static class AdminProductMapper
{
    public static AdminProductDto ToDto(Product e) => new()
    {
        Id = e.Id,
        NameEn = e.Name.Lang("en"),
        NameEt = e.Name.Lang("et"),
        DescriptionEn = e.Description.Lang("en"),
        DescriptionEt = e.Description.Lang("et"),
        MaterialEn = e.Material.Lang("en"),
        MaterialEt = e.Material.Lang("et"),
        Gender = e.Gender.ToString(),
        IsActive = e.IsActive,
        CreatedAt = e.CreatedAt,
        CollectionId = e.CollectionId,
        CollectionName = e.Collection?.Name.Tr(),
        Variants = e.Variants?.Select(ToVariantDto).ToList() ?? [],
        Images = e.Images?.OrderBy(i => i.SortOrder).Select(ToImageDto).ToList() ?? [],
        CategoryIds = e.ProductCategories?.Select(pc => pc.CategoryId).ToList() ?? []
    };

    public static AdminVariantDto ToVariantDto(ProductVariant v) => new()
    {
        Id = v.Id,
        Sku = v.Sku,
        Price = v.Price,
        UnitPrice = v.UnitPrice,
        StockQty = v.StockQty,
        IsActive = v.IsActive,
        ColorId = v.ColorId,
        ColorName = v.Color?.Name.Tr() ?? string.Empty,
        SizeId = v.SizeId,
        SizeCode = v.Size?.SizeCode ?? string.Empty
    };

    public static AdminProductImageDto ToImageDto(ProductImage i) => new()
    {
        Id = i.Id,
        Url = i.Url,
        AltTextEn = i.AltText.Lang("en"),
        AltTextEt = i.AltText.Lang("et"),
        SortOrder = i.SortOrder
    };

    public static void ApplyWrite(AdminProductWriteDto dto, Product entity)
    {
        entity.Name = new LangStr(dto.NameEn, "en") { ["et"] = dto.NameEt };
        entity.Description = new LangStr(dto.DescriptionEn, "en") { ["et"] = dto.DescriptionEt };
        entity.Material = new LangStr(dto.MaterialEn, "en") { ["et"] = dto.MaterialEt };
        entity.Gender = Enum.TryParse<Gender>(dto.Gender, true, out var g) ? g : Gender.NotSpecified;
        entity.IsActive = dto.IsActive;
        entity.CollectionId = dto.CollectionId;
    }

    public static void ApplyWrite(AdminVariantWriteDto dto, ProductVariant entity)
    {
        entity.Sku = dto.Sku;
        entity.Price = dto.Price;
        entity.UnitPrice = dto.UnitPrice;
        entity.StockQty = dto.StockQty;
        entity.IsActive = dto.IsActive;
        entity.ColorId = dto.ColorId;
        entity.SizeId = dto.SizeId;
    }

    public static void ApplyWrite(AdminProductImageDto dto, ProductImage entity)
    {
        entity.Url = dto.Url;
        entity.AltText = new LangStr(dto.AltTextEn, "en") { ["et"] = dto.AltTextEt };
        entity.SortOrder = dto.SortOrder;
    }
}
