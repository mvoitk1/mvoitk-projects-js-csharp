using App.BLL.Contracts;
using App.BLL.Mappers;
using App.DAL.Contracts.UnitOfWork;
using App.BLL.DTO.Collections;

namespace App.BLL.Services;

public class CollectionService(IAppUnitOfWork uow) : ICollectionService
{
    public async Task<IEnumerable<CollectionDto>> GetActiveAsync()
    {
        var collections = await uow.Collections.GetActiveAsync();
        return collections.Select(CollectionMapper.ToDto);
    }
}
