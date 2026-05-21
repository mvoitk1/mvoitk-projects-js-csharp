using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Modules.Infrastructure;
using Npgsql;
using Sales.Application.Contracts;
using Sales.Infrastructure.UnitOfWork;

namespace Sales.Infrastructure;

public static class SalesInfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddSalesInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
                               ?? throw new InvalidOperationException(
                                   "Connection string 'DefaultConnection' not found.");

        // Shared scoped connection so the module contexts can enlist in one transaction.
        services.AddSharedRelationalConnection(_ => new NpgsqlConnection(connectionString));

        services.AddDbContext<SalesDbContext>((sp, options) =>
        {
            options.UseNpgsql(sp.GetRequiredService<DbConnection>())
                .ConfigureWarnings(w =>
                    w.Throw(RelationalEventId.MultipleCollectionIncludeWarning))
                .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTrackingWithIdentityResolution);
        });

        services.AddScoped<ISalesUnitOfWork, SalesUnitOfWork>();
        services.AddModuleTransactionParticipant<SalesDbContext>();
        return services;
    }
}
