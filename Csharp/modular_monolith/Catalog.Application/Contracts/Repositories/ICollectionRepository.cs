using Base.DAL.Contracts;
using Catalog.Domain;

namespace Catalog.Application.Contracts.Repositories;

public interface ICollectionRepository : IBaseRepository<Collection>
{
    Task<IEnumerable<Collection>> GetActiveAsync();
    Task<IEnumerable<Collection>> GetAllOrderedAsync();
}
