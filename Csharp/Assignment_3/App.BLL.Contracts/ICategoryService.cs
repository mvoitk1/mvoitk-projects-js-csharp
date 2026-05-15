using App.BLL.DTO.Categories;

namespace App.BLL.Contracts;

public interface ICategoryService
{
    Task<IEnumerable<CategoryDto>> GetAllAsync();
}
