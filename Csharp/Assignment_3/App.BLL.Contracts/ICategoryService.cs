using App.DTO.v1.Categories;

namespace App.BLL.Services;

public interface ICategoryService
{
    Task<IEnumerable<CategoryDto>> GetAllAsync();
}
