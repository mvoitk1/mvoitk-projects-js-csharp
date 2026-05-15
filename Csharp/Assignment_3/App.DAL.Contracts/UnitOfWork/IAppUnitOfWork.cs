using App.DAL.EF.Repositories;

namespace App.DAL.EF.UnitOfWork;

/// <summary>
/// Single seam between the BLL and the database. Services depend on this interface
/// instead of <see cref="AppDbContext"/>; every use-case calls
/// <see cref="SaveChangesAsync"/> exactly once.
/// </summary>
public interface IAppUnitOfWork
{
    IProductRepository Products { get; }
    ICategoryRepository Categories { get; }
    ICollectionRepository Collections { get; }
    ICartRepository Carts { get; }
    ICartItemRepository CartItems { get; }
    IOrderRepository Orders { get; }
    IProductVariantRepository ProductVariants { get; }
    IProductImageRepository ProductImages { get; }
    IProductCategoryRepository ProductCategories { get; }
    IColorRepository Colors { get; }
    ISizeRepository Sizes { get; }

    Task<int> SaveChangesAsync();
}
