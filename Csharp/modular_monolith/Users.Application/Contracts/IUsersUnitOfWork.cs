namespace Users.Application.Contracts;

public interface IUsersUnitOfWork
{
    IRefreshTokenRepository RefreshTokens { get; }

    Task<int> SaveChangesAsync();
}
