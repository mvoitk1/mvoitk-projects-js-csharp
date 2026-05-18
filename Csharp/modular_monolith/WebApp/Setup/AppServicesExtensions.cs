using App.BLL.Setup;
using App.DAL.EF.Setup;
using Microsoft.Extensions.DependencyInjection;

namespace WebApp.Setup;

public static class AppServicesExtensions
{
    public static IServiceCollection AddAppServices(this IServiceCollection services)
    {
        services.AddAppDataAccess();
        services.AddAppBusinessLogic();
        return services;
    }
}
