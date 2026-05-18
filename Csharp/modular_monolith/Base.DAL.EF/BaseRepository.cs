using Base.Contracts;
using Base.DAL.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Base.DAL.EF;

public class BaseRepository<TEntity, TDbContext> : BaseRepository<TEntity, Guid, TDbContext>, IBaseRepository<TEntity>
    where TEntity : class, IDomainEntityId
    where TDbContext : DbContext
{
    public BaseRepository(TDbContext dbContext) : base(dbContext)
    {
    }
}

public class BaseRepository<TEntity, TKey, TDbContext> : IBaseRepository<TEntity, TKey>
    where TEntity : class, IDomainEntityId<TKey>
    where TKey : IEquatable<TKey>
    where TDbContext : DbContext
{
    protected readonly TDbContext RepoDbContext;
    protected readonly DbSet<TEntity> RepoDbSet;

    public BaseRepository(TDbContext dbContext)
    {
        RepoDbContext = dbContext;
        RepoDbSet = dbContext.Set<TEntity>();
    }

    public virtual async Task<IEnumerable<TEntity>> AllAsync()
    {
        return await RepoDbSet.ToListAsync();
    }

    public virtual async Task<TEntity?> FindAsync(TKey id)
    {
        return await RepoDbSet.FirstOrDefaultAsync(e => e.Id.Equals(id));
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
