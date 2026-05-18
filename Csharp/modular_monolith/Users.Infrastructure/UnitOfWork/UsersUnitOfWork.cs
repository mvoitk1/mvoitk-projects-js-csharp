using Users.Application.Contracts;
using Users.Infrastructure.Repositories;

namespace Users.Infrastructure.UnitOfWork;

public class UsersUnitOfWork : IUsersUnitOfWork
{
    private readonly UsersDbContext _dbContext;

    public UsersUnitOfWork(UsersDbContext dbContext)
    {
        _dbContext = dbContext;
        RefreshTokens = new RefreshTokenRepository(_dbContext);
    }

    public IRefreshTokenRepository RefreshTokens { get; }

    public Task<int> SaveChangesAsync() => _dbContext.SaveChangesAsync();
}
