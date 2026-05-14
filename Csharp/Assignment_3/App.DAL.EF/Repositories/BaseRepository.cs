using App.Domain;
using Microsoft.EntityFrameworkCore;

namespace App.DAL.EF.Repositories;

public class BaseRepository<TEntity> : IBaseRepository<TEntity>
    where TEntity : class, IBaseEntity
{
    protected readonly AppDbContext RepoDbContext;
    protected readonly DbSet<TEntity> RepoDbSet;

    public BaseRepository(AppDbContext dbContext)
    {
        RepoDbContext = dbContext;
        RepoDbSet = dbContext.Set<TEntity>();
    }

    public virtual async Task<IEnumerable<TEntity>> AllAsync()
    {
        return await RepoDbSet.ToListAsync();
    }

    public virtual async Task<TEntity?> FindAsync(Guid id)
    {
        return await RepoDbSet.FirstOrDefaultAsync(e => e.Id == id);
    }

    public virtual void Add(TEntity entity)
    {
        RepoDbSet.Add(entity);
    }

    public virtual void Update(TEntity entity)
    {
        RepoDbSet.Update(entity);
    }

    public virtual void Remove(TEntity entity)
    {
        RepoDbSet.Remove(entity);
    }
}
