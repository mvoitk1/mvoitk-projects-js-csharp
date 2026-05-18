using App.DAL.Contracts.Repositories;

namespace App.DAL.Contracts.UnitOfWork;

/// <summary>
/// Single seam between the BLL and the database. Services depend on this interface
/// instead of the DbContext; every use-case calls <see cref="SaveChangesAsync"/> exactly once.
///
/// Note: refresh-token persistence moved to <c>Users.Application.Contracts.IUsersUnitOfWork</c>
/// after the Users module split.
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
