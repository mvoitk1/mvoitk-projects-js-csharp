using Base.DAL.Contracts;
using Catalog.Domain;
using Catalog.Domain.Enums;

namespace Catalog.Application.Contracts.Repositories;

public interface IProductRepository : IBaseRepository<Product>
{
    Task<IEnumerable<Product>> GetListFilteredAsync(
        Guid? categoryId = null, Guid? collectionId = null, Gender? gender = null);

    Task<Product?> GetWithDetailsAsync(Guid id, bool activeOnly = false);

    Task<IEnumerable<Product>> GetAllWithDetailsAsync();

    Task<Product?> GetWithCategoriesAsync(Guid id);
}
