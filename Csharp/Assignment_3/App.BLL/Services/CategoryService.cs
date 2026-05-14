using App.BLL.Mappers;
using App.DAL.EF.UnitOfWork;
using App.DTO.v1.Categories;

namespace App.BLL.Services;

public class CategoryService(IAppUnitOfWork uow) : ICategoryService
{
    public async Task<IEnumerable<CategoryDto>> GetAllAsync()
    {
        var categories = await uow.Categories.GetRootCategoriesWithSubAsync();
        return categories.Select(CategoryMapper.ToDto);
    }
}
