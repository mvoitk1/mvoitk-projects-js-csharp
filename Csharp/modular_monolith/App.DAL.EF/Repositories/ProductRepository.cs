using App.DAL.Contracts.Repositories;
using Base.DAL.EF;
using App.Domain;
using App.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace App.DAL.EF.Repositories;

public class ProductRepository : BaseRepository<Product, AppDbContext>, IProductRepository
{
    public ProductRepository(AppDbContext dbContext) : base(dbContext)
    {
    }

    public async Task<IEnumerable<Product>> GetListFilteredAsync(
        Guid? categoryId = null, Guid? collectionId = null, Gender? gender = null)
    {
        var query = RepoDbSet
            .Include(p => p.Images)
            .Include(p => p.Variants)
            .Include(p => p.Collection)
            .Include(p => p.ProductCategories)
            .Where(p => p.IsActive)
            .AsQueryable();

        if (categoryId.HasValue)
            query = query.Where(p => p.ProductCategories!.Any(pc => pc.CategoryId == categoryId.Value));

        if (collectionId.HasValue)
            query = query.Where(p => p.CollectionId == collectionId.Value);

        if (gender.HasValue)
            query = query.Where(p => p.Gender == gender.Value);

        return await query.ToListAsync();
    }

    public async Task<Product?> GetWithDetailsAsync(Guid id, bool activeOnly = false)
    {
        var query = RepoDbSet
            .Include(p => p.Images)
            .Include(p => p.Collection)
            .Include(p => p.Variants).ThenInclude(v => v.Color)
            .Include(p => p.Variants).ThenInclude(v => v.Size)
            .Include(p => p.ProductCategories).ThenInclude(pc => pc.Category)
            .AsQueryable();

        if (activeOnly)
            query = query.Where(p => p.IsActive);

        return await query.FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<IEnumerable<Product>> GetAllWithDetailsAsync()
    {
        return await RepoDbSet
            .Include(p => p.Collection)
            .Include(p => p.Variants).ThenInclude(v => v.Color)
            .Include(p => p.Variants).ThenInclude(v => v.Size)
            .Include(p => p.Images)
            .Include(p => p.ProductCategories)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task<Product?> GetWithCategoriesAsync(Guid id)
    {
        return await RepoDbSet
            .Include(p => p.ProductCategories)
            .FirstOrDefaultAsync(p => p.Id == id);
    }
}
