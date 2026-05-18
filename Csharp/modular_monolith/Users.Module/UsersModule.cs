using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Modules.Abstractions;
using Users.Application;
using Users.Infrastructure;
using Users.Web;

namespace Users.Module;

public sealed class UsersModule : IModule
{
    public string Name => "Users";

    public void Register(IServiceCollection services, IConfiguration configuration)
    {
        services.AddUsersInfrastructure(configuration);
        services.AddUsersApplication();
        services.AddUsersWeb();
    }
}
