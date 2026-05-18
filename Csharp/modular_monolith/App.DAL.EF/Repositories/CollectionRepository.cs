using App.DAL.Contracts.Repositories;
using Base.DAL.EF;
using App.Domain;
using Microsoft.EntityFrameworkCore;

namespace App.DAL.EF.Repositories;

public class CollectionRepository : BaseRepository<Collection, AppDbContext>, ICollectionRepository
{
    public CollectionRepository(AppDbContext dbContext) : base(dbContext)
    {
    }

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
