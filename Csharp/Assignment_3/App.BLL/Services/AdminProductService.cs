using App.DAL.EF;
using App.Domain;
using App.Domain.Enums;
using App.DTO.v1.Admin;
using Microsoft.EntityFrameworkCore;

namespace App.BLL.Services;

public class AdminProductService(AppDbContext db) : IAdminProductService
{
    public async Task<IEnumerable<AdminProductDto>> GetAllAsync()
    {
        var products = await db.Products
            .Include(p => p.Collection)
            .Include(p => p.Variants).ThenInclude(v => v.Color)
            .Include(p => p.Variants).ThenInclude(v => v.Size)
            .Include(p => p.Images)
            .Include(p => p.ProductCategories)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        return products.Select(MapToDto);
    }

    public async Task<AdminProductDto?> GetByIdAsync(Guid id)
    {
        var product = await db.Products
            .Include(p => p.Collection)
            .Include(p => p.Variants).ThenInclude(v => v.Color)
            .Include(p => p.Variants).ThenInclude(v => v.Size)
            .Include(p => p.Images)
            .Include(p => p.ProductCategories)
            .FirstOrDefaultAsync(p => p.Id == id);

        return product == null ? null : MapToDto(product);
    }

    public async Task<AdminProductDto> CreateAsync(AdminProductWriteDto dto)
    {
        var product = new Product
        {
            Name = new LangStr(dto.NameEn, "en") { ["et"] = dto.NameEt },
            Description = new LangStr(dto.DescriptionEn, "en") { ["et"] = dto.DescriptionEt },
            Material = new LangStr(dto.MaterialEn, "en") { ["et"] = dto.MaterialEt },
            Gender = Enum.TryParse<Gender>(dto.Gender, true, out var g) ? g : Gender.NotSpecified,
            IsActive = dto.IsActive,
            CollectionId = dto.CollectionId
        };

        if (dto.CategoryIds.Any())
        {
            product.ProductCategories = dto.CategoryIds
                .Select(cId => new ProductCategory { CategoryId = cId })
                .ToList();
        }

        db.Products.Add(product);
        await db.SaveChangesAsync();

        return (await GetByIdAsync(product.Id))!;
    }

    public async Task<AdminProductDto?> UpdateAsync(Guid id, AdminProductWriteDto dto)
    {
        var product = await db.Products
            .Include(p => p.ProductCategories)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product == null) return null;

        product.Name = new LangStr(dto.NameEn, "en") { ["et"] = dto.NameEt };
        product.Description = new LangStr(dto.DescriptionEn, "en") { ["et"] = dto.DescriptionEt };
        product.Material = new LangStr(dto.MaterialEn, "en") { ["et"] = dto.MaterialEt };
        product.Gender = Enum.TryParse<Gender>(dto.Gender, true, out var g) ? g : Gender.NotSpecified;
        product.IsActive = dto.IsActive;
        product.CollectionId = dto.CollectionId;

        // Replace categories
        if (product.ProductCategories != null)
            db.ProductCategories.RemoveRange(product.ProductCategories);

        if (dto.CategoryIds.Any())
        {
            db.ProductCategories.AddRange(dto.CategoryIds
                .Select(cId => new ProductCategory { ProductId = id, CategoryId = cId }));
        }

        await db.SaveChangesAsync();
        return (await GetByIdAsync(id))!;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var product = await db.Products.FindAsync(id);
        if (product == null) return false;

        db.Products.Remove(product);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<AdminVariantDto> AddVariantAsync(Guid productId, AdminVariantWriteDto dto)
    {
        var variant = new ProductVariant
        {
            ProductId = productId,
            Sku = dto.Sku,
            Price = dto.Price,
            UnitPrice = dto.UnitPrice,
            StockQty = dto.StockQty,
            IsActive = dto.IsActive,
            ColorId = dto.ColorId,
            SizeId = dto.SizeId
        };

        db.ProductVariants.Add(variant);
        await db.SaveChangesAsync();

        var created = await db.ProductVariants
            .Include(v => v.Color)
            .Include(v => v.Size)
            .FirstAsync(v => v.Id == variant.Id);

        return MapVariantToDto(created);
    }

    public async Task<AdminVariantDto?> UpdateVariantAsync(Guid productId, Guid variantId, AdminVariantWriteDto dto)
    {
        var variant = await db.ProductVariants
            .Include(v => v.Color)
            .Include(v => v.Size)
            .FirstOrDefaultAsync(v => v.Id == variantId && v.ProductId == productId);

        if (variant == null) return null;

        variant.Sku = dto.Sku;
        variant.Price = dto.Price;
        variant.UnitPrice = dto.UnitPrice;
        variant.StockQty = dto.StockQty;
        variant.IsActive = dto.IsActive;
        variant.ColorId = dto.ColorId;
        variant.SizeId = dto.SizeId;

        await db.SaveChangesAsync();
        return MapVariantToDto(variant);
    }

    public async Task<bool> DeleteVariantAsync(Guid productId, Guid variantId)
    {
        var variant = await db.ProductVariants
            .FirstOrDefaultAsync(v => v.Id == variantId && v.ProductId == productId);

        if (variant == null) return false;
        db.ProductVariants.Remove(variant);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<AdminProductImageDto> AddImageAsync(Guid productId, AdminProductImageDto dto)
    {
        var image = new ProductImage
        {
            ProductId = productId,
            Url = dto.Url,
            AltText = new LangStr(dto.AltTextEn, "en") { ["et"] = dto.AltTextEt },
            SortOrder = dto.SortOrder
        };

        db.ProductImages.Add(image);
        await db.SaveChangesAsync();

        return new AdminProductImageDto
        {
            Id = image.Id,
            Url = image.Url,
            AltTextEn = dto.AltTextEn,
            AltTextEt = dto.AltTextEt,
            SortOrder = image.SortOrder
        };
    }

    public async Task<bool> DeleteImageAsync(Guid productId, Guid imageId)
    {
        var image = await db.ProductImages
            .FirstOrDefaultAsync(i => i.Id == imageId && i.ProductId == productId);

        if (image == null) return false;
        db.ProductImages.Remove(image);
        await db.SaveChangesAsync();
        return true;
    }

    private static AdminProductDto MapToDto(Product p) => new AdminProductDto
    {
        Id = p.Id,
        NameEn = p.Name.ContainsKey("en") ? p.Name["en"] : string.Empty,
        NameEt = p.Name.ContainsKey("et") ? p.Name["et"] : string.Empty,
        DescriptionEn = p.Description.ContainsKey("en") ? p.Description["en"] : string.Empty,
        DescriptionEt = p.Description.ContainsKey("et") ? p.Description["et"] : string.Empty,
        MaterialEn = p.Material.ContainsKey("en") ? p.Material["en"] : string.Empty,
        MaterialEt = p.Material.ContainsKey("et") ? p.Material["et"] : string.Empty,
        Gender = p.Gender.ToString(),
        IsActive = p.IsActive,
        CreatedAt = p.CreatedAt,
        CollectionId = p.CollectionId,
        CollectionName = p.Collection?.Name.Translate(),
        Variants = p.Variants?.Select(MapVariantToDto).ToList() ?? [],
        Images = p.Images?.OrderBy(i => i.SortOrder).Select(i => new AdminProductImageDto
        {
            Id = i.Id,
            Url = i.Url,
            AltTextEn = i.AltText.ContainsKey("en") ? i.AltText["en"] : string.Empty,
            AltTextEt = i.AltText.ContainsKey("et") ? i.AltText["et"] : string.Empty,
            SortOrder = i.SortOrder
        }).ToList() ?? [],
        CategoryIds = p.ProductCategories?.Select(pc => pc.CategoryId).ToList() ?? []
    };

    private static AdminVariantDto MapVariantToDto(ProductVariant v) => new AdminVariantDto
    {
        Id = v.Id,
        Sku = v.Sku,
        Price = v.Price,
        UnitPrice = v.UnitPrice,
        StockQty = v.StockQty,
        IsActive = v.IsActive,
        ColorId = v.ColorId,
        ColorName = v.Color?.Name.Translate() ?? string.Empty,
        SizeId = v.SizeId,
        SizeCode = v.Size?.SizeCode ?? string.Empty
    };
}
