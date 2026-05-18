using Catalog.Domain;

namespace Catalog.Application.Contracts.Repositories;

/// <summary>
/// Join entity with a composite key (no Guid Id), so it does not use IBaseRepository.
/// </summary>
public interface IProductCategoryRepository
{
    void AddRange(IEnumerable<ProductCategory> productCategories);
    void RemoveRange(IEnumerable<ProductCategory> productCategories);
}
