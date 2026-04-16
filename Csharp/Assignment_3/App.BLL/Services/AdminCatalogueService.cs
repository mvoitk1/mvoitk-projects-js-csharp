using App.DAL.EF;
using App.Domain;
using App.DTO.v1.Admin;
using Microsoft.EntityFrameworkCore;

namespace App.BLL.Services;

public class AdminCatalogueService(AppDbContext db) : IAdminCatalogueService
{
    // ─── Categories ──────────────────────────────────────────────────────────

    public async Task<IEnumerable<AdminCategoryDto>> GetAllCategoriesAsync()
    {
        var categories = await db.Categories
            .Include(c => c.ParentCategory)
            .OrderBy(c => c.Name)
            .ToListAsync();

        return categories.Select(MapCategoryToDto);
    }

    public async Task<AdminCategoryDto?> GetCategoryByIdAsync(Guid id)
    {
        var c = await db.Categories.Include(c => c.ParentCategory).FirstOrDefaultAsync(c => c.Id == id);
        return c == null ? null : MapCategoryToDto(c);
    }

    public async Task<AdminCategoryDto> CreateCategoryAsync(AdminCategoryWriteDto dto)
    {
        var category = new Category
        {
            Name = new LangStr(dto.NameEn, "en") { ["et"] = dto.NameEt },
            ParentCategoryId = dto.ParentCategoryId
        };

        db.Categories.Add(category);
        await db.SaveChangesAsync();
        return (await GetCategoryByIdAsync(category.Id))!;
    }

    public async Task<AdminCategoryDto?> UpdateCategoryAsync(Guid id, AdminCategoryWriteDto dto)
    {
        var category = await db.Categories.FindAsync(id);
        if (category == null) return null;

        category.Name = new LangStr(dto.NameEn, "en") { ["et"] = dto.NameEt };
        category.ParentCategoryId = dto.ParentCategoryId;

        await db.SaveChangesAsync();
        return (await GetCategoryByIdAsync(id))!;
    }

    public async Task<bool> DeleteCategoryAsync(Guid id)
    {
        var category = await db.Categories.FindAsync(id);
        if (category == null) return false;

        db.Categories.Remove(category);
        await db.SaveChangesAsync();
        return true;
    }

    // ─── Collections ─────────────────────────────────────────────────────────

    public async Task<IEnumerable<AdminCollectionDto>> GetAllCollectionsAsync()
    {
        var collections = await db.Collections.OrderByDescending(c => c.LaunchDate).ToListAsync();
        return collections.Select(MapCollectionToDto);
    }

    public async Task<AdminCollectionDto?> GetCollectionByIdAsync(Guid id)
    {
        var c = await db.Collections.FindAsync(id);
        return c == null ? null : MapCollectionToDto(c);
    }

    public async Task<AdminCollectionDto> CreateCollectionAsync(AdminCollectionWriteDto dto)
    {
        var collection = new Collection
        {
            Name = new LangStr(dto.NameEn, "en") { ["et"] = dto.NameEt },
            Description = new LangStr(dto.DescriptionEn, "en") { ["et"] = dto.DescriptionEt },
            LaunchDate = dto.LaunchDate,
            IsActive = dto.IsActive
        };

        db.Collections.Add(collection);
        await db.SaveChangesAsync();
        return MapCollectionToDto(collection);
    }

    public async Task<AdminCollectionDto?> UpdateCollectionAsync(Guid id, AdminCollectionWriteDto dto)
    {
        var collection = await db.Collections.FindAsync(id);
        if (collection == null) return null;

        collection.Name = new LangStr(dto.NameEn, "en") { ["et"] = dto.NameEt };
        collection.Description = new LangStr(dto.DescriptionEn, "en") { ["et"] = dto.DescriptionEt };
        collection.LaunchDate = dto.LaunchDate;
        collection.IsActive = dto.IsActive;

        await db.SaveChangesAsync();
        return MapCollectionToDto(collection);
    }

    public async Task<bool> DeleteCollectionAsync(Guid id)
    {
        var collection = await db.Collections.FindAsync(id);
        if (collection == null) return false;

        db.Collections.Remove(collection);
        await db.SaveChangesAsync();
        return true;
    }

    // ─── Stock ───────────────────────────────────────────────────────────────

    public async Task<bool> UpdateStockAsync(Guid variantId, int stockQty)
    {
        var affected = await db.ProductVariants
            .Where(v => v.Id == variantId)
            .ExecuteUpdateAsync(s => s.SetProperty(v => v.StockQty, stockQty));
        return affected > 0;
    }

    // ─── Mappers ─────────────────────────────────────────────────────────────

    private static AdminCategoryDto MapCategoryToDto(Category c) => new AdminCategoryDto
    {
        Id = c.Id,
        NameEn = c.Name.ContainsKey("en") ? c.Name["en"] : string.Empty,
        NameEt = c.Name.ContainsKey("et") ? c.Name["et"] : string.Empty,
        ParentCategoryId = c.ParentCategoryId,
        ParentCategoryName = c.ParentCategory?.Name.Translate()
    };

    private static AdminCollectionDto MapCollectionToDto(Collection c) => new AdminCollectionDto
    {
        Id = c.Id,
        NameEn = c.Name.ContainsKey("en") ? c.Name["en"] : string.Empty,
        NameEt = c.Name.ContainsKey("et") ? c.Name["et"] : string.Empty,
        DescriptionEn = c.Description.ContainsKey("en") ? c.Description["en"] : string.Empty,
        DescriptionEt = c.Description.ContainsKey("et") ? c.Description["et"] : string.Empty,
        LaunchDate = c.LaunchDate,
        IsActive = c.IsActive
    };
}
