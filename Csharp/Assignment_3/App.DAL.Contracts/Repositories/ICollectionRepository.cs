using Base.DAL.Contracts;
using App.Domain;

namespace App.DAL.Contracts.Repositories;

public interface ICollectionRepository : IBaseRepository<Collection>
{
    /// <summary>Active collections ordered by launch date, for the public shop.</summary>
    Task<IEnumerable<Collection>> GetActiveAsync();

    /// <summary>All collections, newest launch date first (admin back-office).</summary>
    Task<IEnumerable<Collection>> GetAllOrderedAsync();
}
