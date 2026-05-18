using Catalog.Application.Contracts.Repositories;
using Catalog.Domain;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Infrastructure.Repositories;

public class ProductCategoryRepository : IProductCategoryRepository
{
    private readonly DbSet<ProductCategory> _dbSet;

    public ProductCategoryRepository(CatalogDbContext dbContext)
    {
        _dbSet = dbContext.Set<ProductCategory>();
    }

    public void AddRange(IEnumerable<ProductCategory> productCategories)
    {
        _dbSet.AddRange(productCategories);
    }

    public void RemoveRange(IEnumerable<ProductCategory> productCategories)
    {
        _dbSet.RemoveRange(productCategories);
    }
}
