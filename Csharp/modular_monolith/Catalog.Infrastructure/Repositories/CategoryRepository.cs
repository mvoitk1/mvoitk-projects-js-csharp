using Base.DAL.EF;
using Catalog.Application.Contracts.Repositories;
using Catalog.Domain;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Infrastructure.Repositories;

public class CategoryRepository(CatalogDbContext dbContext)
    : BaseRepository<Category, CatalogDbContext>(dbContext), ICategoryRepository
{
    public async Task<IEnumerable<Category>> GetRootCategoriesWithSubAsync()
    {
        return await RepoDbSet
            .Include(c => c.SubCategories)
            .Where(c => c.ParentCategoryId == null)
            .ToListAsync();
    }

    public async Task<IEnumerable<Category>> GetAllWithParentAsync()
    {
        return await RepoDbSet
            .Include(c => c.ParentCategory)
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<Category?> GetWithParentAsync(Guid id)
    {
        return await RepoDbSet
            .Include(c => c.ParentCategory)
            .FirstOrDefaultAsync(c => c.Id == id);
    }
}
