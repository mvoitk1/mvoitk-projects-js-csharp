using App.BLL.Mappers;
using App.DAL.EF.UnitOfWork;
using App.DTO.v1.Collections;

namespace App.BLL.Services;

public class CollectionService(IAppUnitOfWork uow) : ICollectionService
{
    public async Task<IEnumerable<CollectionDto>> GetActiveAsync()
    {
        var collections = await uow.Collections.GetActiveAsync();
        return collections.Select(CollectionMapper.ToDto);
    }
}
