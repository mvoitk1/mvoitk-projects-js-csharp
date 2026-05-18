using Base.DAL.Contracts;
using Catalog.Domain;

namespace Catalog.Application.Contracts.Repositories;

public interface ICategoryRepository : IBaseRepository<Category>
{
    Task<IEnumerable<Category>> GetRootCategoriesWithSubAsync();
    Task<IEnumerable<Category>> GetAllWithParentAsync();
    Task<Category?> GetWithParentAsync(Guid id);
}
