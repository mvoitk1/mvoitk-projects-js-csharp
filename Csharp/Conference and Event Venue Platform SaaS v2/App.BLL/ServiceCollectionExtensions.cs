using App.BLL.Services;
using Microsoft.Extensions.DependencyInjection;

namespace App.BLL;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddVenuePlatformServices(this IServiceCollection services)
    {
        services.AddScoped<IPublicVenueDiscoveryService, PublicVenueDiscoveryService>();
        services.AddScoped<IEmployeeWorkspaceService, EmployeeWorkspaceService>();
        services.AddScoped<IVenueMembershipService, VenueMembershipService>();
        services.AddScoped<IVenueAdminService, VenueAdminService>();

        return services;
    }
}
