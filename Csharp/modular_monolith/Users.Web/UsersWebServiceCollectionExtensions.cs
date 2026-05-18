using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.DependencyInjection;

namespace Users.Web;

public static class UsersWebServiceCollectionExtensions
{
    /// <summary>
    /// Registers this module's controllers + views with the MVC pipeline.
    /// </summary>
    public static IServiceCollection AddUsersWeb(this IServiceCollection services)
    {
        var assembly = typeof(UsersWebServiceCollectionExtensions).Assembly;
        services.AddControllersWithViews()
            .PartManager.ApplicationParts.Add(new AssemblyPart(assembly));
        return services;
    }
}

/// <summary>Marker for assembly scanning in tests.</summary>
public sealed class UsersWebAssemblyMarker { }
