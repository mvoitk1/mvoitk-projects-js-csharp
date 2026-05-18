using Catalog.Application.Dtos.Collections;

namespace Catalog.Application.Contracts;

public interface ICollectionService
{
    Task<IEnumerable<CollectionDto>> GetActiveAsync();
}
