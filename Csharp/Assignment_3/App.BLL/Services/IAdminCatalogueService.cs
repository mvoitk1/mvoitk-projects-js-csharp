using App.DTO.v1.Admin;

namespace App.BLL.Services;

public interface IAdminCatalogueService
{
    // Categories
    Task<IEnumerable<AdminCategoryDto>> GetAllCategoriesAsync();
    Task<AdminCategoryDto?> GetCategoryByIdAsync(Guid id);
    Task<AdminCategoryDto> CreateCategoryAsync(AdminCategoryWriteDto dto);
    Task<AdminCategoryDto?> UpdateCategoryAsync(Guid id, AdminCategoryWriteDto dto);
    Task<bool> DeleteCategoryAsync(Guid id);

    // Collections
    Task<IEnumerable<AdminCollectionDto>> GetAllCollectionsAsync();
    Task<AdminCollectionDto?> GetCollectionByIdAsync(Guid id);
    Task<AdminCollectionDto> CreateCollectionAsync(AdminCollectionWriteDto dto);
    Task<AdminCollectionDto?> UpdateCollectionAsync(Guid id, AdminCollectionWriteDto dto);
    Task<bool> DeleteCollectionAsync(Guid id);

    // Stock
    Task<bool> UpdateStockAsync(Guid variantId, int stockQty);
}
