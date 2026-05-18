using Catalog.Application.Contracts;
using Catalog.Application.Dtos.Categories;
using Catalog.Application.Mappers;

namespace Catalog.Application.Services;

public class CategoryService(ICatalogUnitOfWork uow) : ICategoryService
{
    public async Task<IEnumerable<CategoryDto>> GetAllAsync()
    {
        var categories = await uow.Categories.GetRootCategoriesWithSubAsync();
        return categories.Select(CategoryMapper.ToDto);
    }
}
