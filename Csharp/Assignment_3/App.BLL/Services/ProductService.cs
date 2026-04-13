using App.DAL.EF;
using App.Domain.Enums;
using App.DTO.v1.Products;
using Microsoft.EntityFrameworkCore;

namespace App.BLL.Services;

public class ProductService(AppDbContext db) : IProductService
{
    public async Task<IEnumerable<ProductListItemDto>> GetListAsync(
        Guid? categoryId = null, Guid? collectionId = null, string? gender = null)
    {
        var query = db.Products
            .Include(p => p.Images)
            .Include(p => p.Variants)
            .Include(p => p.Collection)
            .Include(p => p.ProductCategories)
            .Where(p => p.IsActive)
            .AsQueryable();

        if (categoryId.HasValue)
        {
            query = query.Where(p => p.ProductCategories!.Any(pc => pc.CategoryId == categoryId.Value));
        }

        if (collectionId.HasValue)
        {
            query = query.Where(p => p.CollectionId == collectionId.Value);
        }

        if (!string.IsNullOrEmpty(gender) && Enum.TryParse<Gender>(gender, true, out var genderEnum))
        {
            query = query.Where(p => p.Gender == genderEnum);
        }

        var products = await query.ToListAsync();

        return products.Select(p => new ProductListItemDto
        {
            Id = p.Id,
            Name = p.Name.Translate() ?? string.Empty,
            Gender = p.Gender.ToString(),
            IsActive = p.IsActive,
            LowestPrice = p.Variants?.Where(v => v.IsActive).Min(v => (decimal?)v.Price),
            PrimaryImageUrl = p.Images?.OrderBy(i => i.SortOrder).FirstOrDefault()?.Url,
            CollectionName = p.Collection?.Name.Translate()
        });
    }

    public async Task<ProductDto?> GetByIdAsync(Guid id)
    {
        var product = await db.Products
            .Include(p => p.Images)
            .Include(p => p.Collection)
            .Include(p => p.Variants)
                .ThenInclude(v => v.Color)
            .Include(p => p.Variants)
                .ThenInclude(v => v.Size)
            .Include(p => p.ProductCategories)
                .ThenInclude(pc => pc.Category)
            .FirstOrDefaultAsync(p => p.Id == id && p.IsActive);

        if (product == null) return null;

        return new ProductDto
        {
            Id = product.Id,
            Name = product.Name.Translate() ?? string.Empty,
            Description = product.Description.Translate() ?? string.Empty,
            Material = product.Material.Translate() ?? string.Empty,
            Gender = product.Gender.ToString(),
            IsActive = product.IsActive,
            CreatedAt = product.CreatedAt,
            CollectionId = product.CollectionId,
            CollectionName = product.Collection?.Name.Translate(),
            Images = product.Images?
                .OrderBy(i => i.SortOrder)
                .Select(i => new ProductImageDto
                {
                    Id = i.Id,
                    Url = i.Url,
                    AltText = i.AltText.Translate() ?? string.Empty,
                    SortOrder = i.SortOrder
                }).ToList() ?? [],
            Variants = product.Variants?
                .Where(v => v.IsActive)
                .Select(v => new ProductVariantDto
                {
                    Id = v.Id,
                    Sku = v.Sku,
                    Price = v.Price,
                    StockQty = v.StockQty,
                    IsActive = v.IsActive,
                    ColorId = v.ColorId,
                    ColorName = v.Color?.Name.Translate() ?? string.Empty,
                    ColorHex = v.Color?.HexCode ?? string.Empty,
                    SizeId = v.SizeId,
                    SizeCode = v.Size?.SizeCode ?? string.Empty,
                    SizeDisplayName = v.Size?.DisplayName.Translate() ?? string.Empty
                }).ToList() ?? [],
            CategoryNames = product.ProductCategories?
                .Select(pc => pc.Category?.Name.Translate() ?? string.Empty)
                .ToList() ?? []
        };
    }
}
