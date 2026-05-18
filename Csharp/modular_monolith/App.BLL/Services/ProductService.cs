using App.BLL.Contracts;
using App.BLL.Mappers;
using App.DAL.Contracts.UnitOfWork;
using App.Domain.Enums;
using App.BLL.DTO.Products;

namespace App.BLL.Services;

public class ProductService(IAppUnitOfWork uow) : IProductService
{
    public async Task<IEnumerable<ProductListItemDto>> GetListAsync(
        Guid? categoryId = null, Guid? collectionId = null, string? gender = null)
    {
        Gender? genderFilter = null;
        if (!string.IsNullOrEmpty(gender) && Enum.TryParse<Gender>(gender, true, out var genderEnum))
            genderFilter = genderEnum;

        var products = await uow.Products.GetListFilteredAsync(categoryId, collectionId, genderFilter);

        return products.Select(ProductMapper.ToListItem);
    }

    public async Task<ProductDto?> GetByIdAsync(Guid id)
    {
        var product = await uow.Products.GetWithDetailsAsync(id, activeOnly: true);
        return product == null ? null : ProductMapper.ToDto(product);
    }
}
