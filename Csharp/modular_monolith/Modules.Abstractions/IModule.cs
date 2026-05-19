using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

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

    void RegisterHealthChecks(IHealthChecksBuilder builder) { }

    Task SeedAsync(IServiceProvider services, IConfiguration configuration, CancellationToken ct)
        => Task.CompletedTask;
}
