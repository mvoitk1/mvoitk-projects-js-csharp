using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Modules.Abstractions;

/// <summary>
/// Implemented by every module's bootstrap class. The composition root calls
/// <see cref="Register"/> at startup, optionally followed by <see cref="MapEndpoints"/>
/// after the app is built.
/// </summary>
public interface IModule
{
    string Name { get; }

    void Register(IServiceCollection services, IConfiguration configuration);

    void MapEndpoints(IEndpointRouteBuilder endpoints) { }
}
