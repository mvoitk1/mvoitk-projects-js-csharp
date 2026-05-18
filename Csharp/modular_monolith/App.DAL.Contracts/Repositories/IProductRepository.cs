using Base.DAL.Contracts;
using App.Domain;
using App.Domain.Enums;

namespace App.DAL.Contracts.Repositories;

public interface IProductRepository : IBaseRepository<Product>
{
    /// <summary>Active products for the public shop, filtered by optional category/collection/gender.</summary>
    Task<IEnumerable<Product>> GetListFilteredAsync(
        Guid? categoryId = null, Guid? collectionId = null, Gender? gender = null);

    /// <summary>Single product with the full include graph (variants, images, categories, collection).</summary>
    Task<Product?> GetWithDetailsAsync(Guid id, bool activeOnly = false);

    /// <summary>All products with the full include graph, newest first (admin back-office).</summary>
    Task<IEnumerable<Product>> GetAllWithDetailsAsync();

    /// <summary>Product with its ProductCategories loaded, for category re-assignment on update.</summary>
    Task<Product?> GetWithCategoriesAsync(Guid id);
}
