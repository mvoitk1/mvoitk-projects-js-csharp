using App.BLL.DTO.Collections;

namespace App.BLL.Contracts;

public interface ICollectionService
{
    Task<IEnumerable<CollectionDto>> GetActiveAsync();
}
