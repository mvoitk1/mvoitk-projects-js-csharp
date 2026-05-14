using App.Domain;

namespace App.DAL.EF.Repositories;

public interface IBaseRepository<TEntity>
    where TEntity : class, IBaseEntity
{
    Task<IEnumerable<TEntity>> AllAsync();
    Task<TEntity?> FindAsync(Guid id);
    void Add(TEntity entity);
    void Update(TEntity entity);
    void Remove(TEntity entity);
}
