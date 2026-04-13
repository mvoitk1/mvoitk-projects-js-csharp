using App.DTO.v1.Admin;

namespace App.BLL.Services;

public interface IAdminProductService
{
    Task<IEnumerable<AdminProductDto>> GetAllAsync();
    Task<AdminProductDto?> GetByIdAsync(Guid id);
    Task<AdminProductDto> CreateAsync(AdminProductWriteDto dto);
    Task<AdminProductDto?> UpdateAsync(Guid id, AdminProductWriteDto dto);
    Task<bool> DeleteAsync(Guid id);

    Task<AdminVariantDto> AddVariantAsync(Guid productId, AdminVariantWriteDto dto);
    Task<AdminVariantDto?> UpdateVariantAsync(Guid productId, Guid variantId, AdminVariantWriteDto dto);
    Task<bool> DeleteVariantAsync(Guid productId, Guid variantId);

    Task<AdminProductImageDto> AddImageAsync(Guid productId, AdminProductImageDto dto);
    Task<bool> DeleteImageAsync(Guid productId, Guid imageId);
}
