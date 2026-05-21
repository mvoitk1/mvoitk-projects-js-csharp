using System.Data.Common;
using Catalog.Application.Contracts;
using Catalog.Infrastructure.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Modules.Infrastructure;
using Npgsql;

namespace Catalog.Infrastructure;

public static class CatalogInfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddCatalogInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
                               ?? throw new InvalidOperationException(
                                   "Connection string 'DefaultConnection' not found.");

        // Shared scoped connection so the module contexts can enlist in one transaction.
        services.AddSharedRelationalConnection(_ => new NpgsqlConnection(connectionString));

        services.AddDbContext<CatalogDbContext>((sp, options) =>
        {
            options.UseNpgsql(
                    sp.GetRequiredService<DbConnection>(),
                    o => o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery))
                .ConfigureWarnings(w =>
                    w.Throw(RelationalEventId.MultipleCollectionIncludeWarning))
                .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTrackingWithIdentityResolution);
        });

        services.AddScoped<ICatalogUnitOfWork, CatalogUnitOfWork>();
        services.AddModuleTransactionParticipant<CatalogDbContext>();
        return services;
    }
}
