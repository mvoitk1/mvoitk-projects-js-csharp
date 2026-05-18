using Microsoft.Extensions.DependencyInjection;
using Users.Application.Contracts;
using Users.Application.Services;

namespace Users.Application;

public static class UsersApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddUsersApplication(this IServiceCollection services)
    {
        services.AddScoped<IIdentityService, IdentityService>();
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssemblyContaining<UsersApplicationAssemblyMarker>());
        return services;
    }
}

/// <summary>Used by MediatR + tests to scan this assembly.</summary>
public sealed class UsersApplicationAssemblyMarker { }
