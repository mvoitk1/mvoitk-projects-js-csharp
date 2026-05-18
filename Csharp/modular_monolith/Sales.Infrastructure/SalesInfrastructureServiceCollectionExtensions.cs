using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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

        services.AddDbContext<SalesDbContext>(options =>
        {
            options.UseNpgsql(connectionString)
                .ConfigureWarnings(w =>
                    w.Throw(RelationalEventId.MultipleCollectionIncludeWarning))
                .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTrackingWithIdentityResolution);
        });

        services.AddScoped<ISalesUnitOfWork, SalesUnitOfWork>();
        return services;
    }
}
