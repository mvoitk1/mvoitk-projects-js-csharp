namespace Base.DAL.Contracts;

public interface IBaseUnitOfWork
{
    Task<int> SaveChangesAsync();
}
