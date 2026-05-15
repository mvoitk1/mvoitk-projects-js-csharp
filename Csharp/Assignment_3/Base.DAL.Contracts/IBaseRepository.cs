using Base.Contracts;

namespace Base.DAL.Contracts;

public interface IBaseRepository<TEntity> : IBaseRepository<TEntity, Guid>
    where TEntity : class, IDomainEntityId
{
}

public interface IBaseRepository<TEntity, TKey>
    where TEntity : class, IDomainEntityId<TKey>
    where TKey : IEquatable<TKey>
{
    Task<IEnumerable<TEntity>> AllAsync();
    Task<TEntity?> FindAsync(TKey id);
    void Add(TEntity entity);
    void Update(TEntity entity);
    void Remove(TEntity entity);
}
