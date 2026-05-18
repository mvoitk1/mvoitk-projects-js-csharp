using Base.DAL.EF;
using Catalog.Application.Contracts.Repositories;
using Catalog.Domain;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Infrastructure.Repositories;

public class CollectionRepository(CatalogDbContext dbContext)
    : BaseRepository<Collection, CatalogDbContext>(dbContext), ICollectionRepository
{
    public async Task<IEnumerable<Collection>> GetActiveAsync()
    {
        return await RepoDbSet
            .Where(c => c.IsActive)
            .OrderBy(c => c.LaunchDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<Collection>> GetAllOrderedAsync()
    {
        return await RepoDbSet
            .OrderByDescending(c => c.LaunchDate)
            .ToListAsync();
    }
}
