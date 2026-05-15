using App.DTO.v1.Collections;

namespace App.BLL.Services;

public interface ICollectionService
{
    Task<IEnumerable<CollectionDto>> GetActiveAsync();
}
