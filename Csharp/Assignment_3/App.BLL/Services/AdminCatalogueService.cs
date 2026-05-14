using App.BLL.Mappers;
using App.DAL.EF.UnitOfWork;
using App.Domain;
using App.DTO.v1.Admin;

namespace App.BLL.Services;

public class AdminCatalogueService(IAppUnitOfWork uow) : IAdminCatalogueService
{
    // ─── Categories ──────────────────────────────────────────────────────────

    public async Task<IEnumerable<AdminCategoryDto>> GetAllCategoriesAsync()
    {
        var categories = await uow.Categories.GetAllWithParentAsync();
        return categories.Select(CategoryMapper.ToAdminDto);
    }

    public async Task<AdminCategoryDto?> GetCategoryByIdAsync(Guid id)
    {
        var c = await uow.Categories.GetWithParentAsync(id);
        return c == null ? null : CategoryMapper.ToAdminDto(c);
    }

    public async Task<AdminCategoryDto> CreateCategoryAsync(AdminCategoryWriteDto dto)
    {
        var category = new Category();
        CategoryMapper.ApplyWrite(dto, category);

        uow.Categories.Add(category);
        await uow.SaveChangesAsync();
        return (await GetCategoryByIdAsync(category.Id))!;
    }

    public async Task<AdminCategoryDto?> UpdateCategoryAsync(Guid id, AdminCategoryWriteDto dto)
    {
        var category = await uow.Categories.FindAsync(id);
        if (category == null) return null;

        CategoryMapper.ApplyWrite(dto, category);
        await uow.SaveChangesAsync();
        return (await GetCategoryByIdAsync(id))!;
    }

    public async Task<bool> DeleteCategoryAsync(Guid id)
    {
        var category = await uow.Categories.FindAsync(id);
        if (category == null) return false;

        uow.Categories.Remove(category);
        await uow.SaveChangesAsync();
        return true;
    }

    // ─── Collections ─────────────────────────────────────────────────────────

    public async Task<IEnumerable<AdminCollectionDto>> GetAllCollectionsAsync()
    {
        var collections = await uow.Collections.GetAllOrderedAsync();
        return collections.Select(CollectionMapper.ToAdminDto);
    }

    public async Task<AdminCollectionDto?> GetCollectionByIdAsync(Guid id)
    {
        var c = await uow.Collections.FindAsync(id);
        return c == null ? null : CollectionMapper.ToAdminDto(c);
    }

    public async Task<AdminCollectionDto> CreateCollectionAsync(AdminCollectionWriteDto dto)
    {
        var collection = new Collection();
        CollectionMapper.ApplyWrite(dto, collection);

        uow.Collections.Add(collection);
        await uow.SaveChangesAsync();
        return CollectionMapper.ToAdminDto(collection);
    }

    public async Task<AdminCollectionDto?> UpdateCollectionAsync(Guid id, AdminCollectionWriteDto dto)
    {
        var collection = await uow.Collections.FindAsync(id);
        if (collection == null) return null;

        CollectionMapper.ApplyWrite(dto, collection);
        await uow.SaveChangesAsync();
        return CollectionMapper.ToAdminDto(collection);
    }

    public async Task<bool> DeleteCollectionAsync(Guid id)
    {
        var collection = await uow.Collections.FindAsync(id);
        if (collection == null) return false;

        uow.Collections.Remove(collection);
        await uow.SaveChangesAsync();
        return true;
    }

    // ─── Stock ───────────────────────────────────────────────────────────────

    public async Task<bool> UpdateStockAsync(Guid variantId, int stockQty)
    {
        var variant = await uow.ProductVariants.FindAsync(variantId);
        if (variant == null) return false;

        variant.StockQty = stockQty;
        await uow.SaveChangesAsync();
        return true;
    }
}
