using Base.DAL.Contracts;
using App.Domain;

namespace App.DAL.Contracts.Repositories;

public interface ICategoryRepository : IBaseRepository<Category>
{
    /// <summary>Root categories (no parent) with their sub-categories, for the public catalogue tree.</summary>
    Task<IEnumerable<Category>> GetRootCategoriesWithSubAsync();

    /// <summary>All categories with parent loaded, ordered by name (admin back-office).</summary>
    Task<IEnumerable<Category>> GetAllWithParentAsync();

    /// <summary>Single category with its parent loaded.</summary>
    Task<Category?> GetWithParentAsync(Guid id);
}
