using Catalog.Application.Contracts;
using Catalog.Application.Contracts.Repositories;
using Catalog.Infrastructure.Repositories;

namespace Catalog.Infrastructure.UnitOfWork;

public class CatalogUnitOfWork : ICatalogUnitOfWork
{
    private readonly CatalogDbContext _dbContext;

    public CatalogUnitOfWork(CatalogDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    private IProductRepository? _products;
    public IProductRepository Products => _products ??= new ProductRepository(_dbContext);

    private ICategoryRepository? _categories;
    public ICategoryRepository Categories => _categories ??= new CategoryRepository(_dbContext);

    private ICollectionRepository? _collections;
    public ICollectionRepository Collections => _collections ??= new CollectionRepository(_dbContext);

    private IProductVariantRepository? _productVariants;
    public IProductVariantRepository ProductVariants =>
        _productVariants ??= new ProductVariantRepository(_dbContext);

    private IProductImageRepository? _productImages;
    public IProductImageRepository ProductImages =>
        _productImages ??= new ProductImageRepository(_dbContext);

    private IProductCategoryRepository? _productCategories;
    public IProductCategoryRepository ProductCategories =>
        _productCategories ??= new ProductCategoryRepository(_dbContext);

    private IColorRepository? _colors;
    public IColorRepository Colors => _colors ??= new ColorRepository(_dbContext);

    private ISizeRepository? _sizes;
    public ISizeRepository Sizes => _sizes ??= new SizeRepository(_dbContext);

    public Task<int> SaveChangesAsync() => _dbContext.SaveChangesAsync();
}
