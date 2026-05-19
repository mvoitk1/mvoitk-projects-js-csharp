using Microsoft.Extensions.DependencyInjection;

namespace WebApp.Setup;

public static class AppServicesExtensions
{
    public static IServiceCollection AddAppServices(this IServiceCollection services)
    {
        return services;
    }
}
