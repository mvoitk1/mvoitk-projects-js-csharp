using App.DTO.v1.Products;

namespace App.BLL.Services;

public interface IProductService
{
    Task<IEnumerable<ProductListItemDto>> GetListAsync(Guid? categoryId = null, Guid? collectionId = null, string? gender = null);
    Task<ProductDto?> GetByIdAsync(Guid id);
}
