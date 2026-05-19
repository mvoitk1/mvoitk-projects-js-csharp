using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Tests.Shared;
using Xunit;

namespace WebApp.Tests.Integration;

[Collection("Database tests")]
public class IntegrationTestHomeController : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory<Program> _factory;

    public IntegrationTestHomeController(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task Get_Index_IsSuccessful()
    {
        var response = await _client.GetAsync("/");
        response.EnsureSuccessStatusCode();
    }
}
