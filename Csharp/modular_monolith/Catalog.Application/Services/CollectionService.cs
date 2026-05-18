using Catalog.Application.Contracts;
using Catalog.Application.Dtos.Collections;
using Catalog.Application.Mappers;

namespace Catalog.Application.Services;

public class CollectionService(ICatalogUnitOfWork uow) : ICollectionService
{
    public async Task<IEnumerable<CollectionDto>> GetActiveAsync()
    {
        var collections = await uow.Collections.GetActiveAsync();
        return collections.Select(CollectionMapper.ToDto);
    }
}
