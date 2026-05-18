using Catalog.Application.Contracts.Repositories;

namespace Catalog.Application.Contracts;

public interface ICatalogUnitOfWork
{
    IProductRepository Products { get; }
    ICategoryRepository Categories { get; }
    ICollectionRepository Collections { get; }
    IProductVariantRepository ProductVariants { get; }
    IProductImageRepository ProductImages { get; }
    IProductCategoryRepository ProductCategories { get; }
    IColorRepository Colors { get; }
    ISizeRepository Sizes { get; }

    Task<int> SaveChangesAsync();
}
