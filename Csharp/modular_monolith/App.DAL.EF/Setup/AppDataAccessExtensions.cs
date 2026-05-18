using App.DAL.Contracts.UnitOfWork;
using App.DAL.EF.UnitOfWork;
using Microsoft.Extensions.DependencyInjection;

namespace App.DAL.EF.Setup;

public static class AppDataAccessExtensions
{
    public static IServiceCollection AddAppDataAccess(this IServiceCollection services)
    {
        services.AddScoped<IAppUnitOfWork, AppUnitOfWork>();
        return services;
    }
}
