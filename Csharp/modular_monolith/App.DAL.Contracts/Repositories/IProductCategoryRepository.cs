using App.Domain;

namespace App.DAL.Contracts.Repositories;

/// <summary>
/// ProductCategory is a join entity with a composite key (no Guid Id), so it does not
/// use <see cref="IBaseRepository{TEntity}"/>.
/// </summary>
public interface IProductCategoryRepository
{
    void AddRange(IEnumerable<ProductCategory> productCategories);
    void RemoveRange(IEnumerable<ProductCategory> productCategories);
}
