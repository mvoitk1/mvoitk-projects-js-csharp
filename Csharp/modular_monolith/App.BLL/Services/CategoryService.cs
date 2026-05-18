using App.BLL.Contracts;
using App.BLL.Mappers;
using App.DAL.Contracts.UnitOfWork;
using App.BLL.DTO.Categories;

namespace App.BLL.Services;

public class CategoryService(IAppUnitOfWork uow) : ICategoryService
{
    public async Task<IEnumerable<CategoryDto>> GetAllAsync()
    {
        var categories = await uow.Categories.GetRootCategoriesWithSubAsync();
        return categories.Select(CategoryMapper.ToDto);
    }
}
