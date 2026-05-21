using System.Collections.Generic;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Modules.Contracts.Users.Queries;
using Users.Application;
using Users.Infrastructure;
using Xunit;

namespace Modules.Users.Tests.DependencyInjection;

public class CrossModuleHandlerRegistrationTests
{
    [Fact]
    public void GetUserSnapshotQueryHandler_IsRegistered()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=x;Username=x",
                ["JWT:Key"] = new string('k', 64),
                ["JWT:Issuer"] = "test",
                ["JWT:Audience"] = "test",
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddUsersApplication();
        services.AddUsersInfrastructure(config);

        // The cross-module query handler lives in Users.Infrastructure; if MediatR
        // doesn't scan that assembly the admin orders page 500s with "no handler".
        Assert.Contains(services, d =>
            d.ServiceType == typeof(IRequestHandler<GetUserSnapshotQuery, UserSnapshotDto?>));
    }
}
