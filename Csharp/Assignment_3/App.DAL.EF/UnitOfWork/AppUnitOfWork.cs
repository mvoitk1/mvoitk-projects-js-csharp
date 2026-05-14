using App.DAL.EF.Repositories;

namespace App.DAL.EF.UnitOfWork;

public class AppUnitOfWork : IAppUnitOfWork
{
    private readonly AppDbContext _dbContext;

    public AppUnitOfWork(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    private IProductRepository? _products;
    public IProductRepository Products => _products ??= new ProductRepository(_dbContext);

    private ICategoryRepository? _categories;
    public ICategoryRepository Categories => _categories ??= new CategoryRepository(_dbContext);

    private ICollectionRepository? _collections;
    public ICollectionRepository Collections => _collections ??= new CollectionRepository(_dbContext);

    private ICartRepository? _carts;
    public ICartRepository Carts => _carts ??= new CartRepository(_dbContext);

    private ICartItemRepository? _cartItems;
    public ICartItemRepository CartItems => _cartItems ??= new CartItemRepository(_dbContext);

    private IOrderRepository? _orders;
    public IOrderRepository Orders => _orders ??= new OrderRepository(_dbContext);

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

    public async Task<int> SaveChangesAsync()
    {
        return await _dbContext.SaveChangesAsync();
    }
}
