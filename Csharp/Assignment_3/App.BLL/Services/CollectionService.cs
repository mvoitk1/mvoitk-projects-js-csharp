using App.DAL.EF;
using App.DTO.v1.Collections;
using Microsoft.EntityFrameworkCore;

namespace App.BLL.Services;

public class CollectionService(AppDbContext db) : ICollectionService
{
    public async Task<IEnumerable<CollectionDto>> GetActiveAsync()
    {
        var collections = await db.Collections
            .Where(c => c.IsActive)
            .OrderBy(c => c.LaunchDate)
            .ToListAsync();

        return collections.Select(c => new CollectionDto
        {
            Id = c.Id,
            Name = c.Name.Translate() ?? string.Empty,
            Description = c.Description.Translate() ?? string.Empty,
            LaunchDate = c.LaunchDate,
            IsActive = c.IsActive
        });
    }
}
