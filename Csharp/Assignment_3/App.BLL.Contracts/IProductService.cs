using App.BLL.DTO.Products;

namespace App.BLL.Contracts;

public interface IProductService
{
    Task<IEnumerable<ProductListItemDto>> GetListAsync(Guid? categoryId = null, Guid? collectionId = null, string? gender = null);
    Task<ProductDto?> GetByIdAsync(Guid id);
}
