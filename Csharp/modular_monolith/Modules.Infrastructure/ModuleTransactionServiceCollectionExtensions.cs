using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Modules.SharedKernel;

namespace Modules.Infrastructure;

public static class ModuleTransactionServiceCollectionExtensions
{
    /// <summary>
    /// Registers the single scoped <see cref="DbConnection"/> the module contexts
    /// share, so they can enlist in one transaction. Idempotent — call from every
    /// module's infrastructure; the first registration wins.
    /// </summary>
    public static IServiceCollection AddSharedRelationalConnection(
        this IServiceCollection services, Func<IServiceProvider, DbConnection> connectionFactory)
    {
        services.TryAddScoped(connectionFactory);
        return services;
    }

    /// <summary>
    /// Enlists a module's context in the app-level <see cref="IUnitOfWorkScope"/>.
    /// </summary>
    public static IServiceCollection AddModuleTransactionParticipant<TContext>(this IServiceCollection services)
        where TContext : DbContext
    {
        services.TryAddScoped<IUnitOfWorkScope, RelationalUnitOfWorkScope>();
        services.AddScoped<ITransactionParticipant, DbContextTransactionParticipant<TContext>>();
        return services;
    }
}
