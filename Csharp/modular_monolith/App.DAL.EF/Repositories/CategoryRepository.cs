using App.DAL.Contracts.Repositories;
using Base.DAL.EF;
using App.Domain;
using Microsoft.EntityFrameworkCore;

namespace App.DAL.EF.Repositories;

public class CategoryRepository : BaseRepository<Category, AppDbContext>, ICategoryRepository
{
    public CategoryRepository(AppDbContext dbContext) : base(dbContext)
    {
    }

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
