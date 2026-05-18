using Catalog.Application.Dtos.Categories;

namespace Catalog.Application.Contracts;

public interface ICategoryService
{
    Task<IEnumerable<CategoryDto>> GetAllAsync();
}
