using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Tests.Shared;
using Xunit;

namespace WebApp.Tests.Integration.Views;

[Collection("Database tests")]
public class ShopViewTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ShopViewTests(CustomWebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task ShopIndex_Anonymous_ReturnsOk()
    {
        var response = await _client.GetAsync("/Shop");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.False(string.IsNullOrWhiteSpace(html));
    }

    [Fact]
    public async Task ShopDetail_UnknownProductId_Returns404()
    {
        var response = await _client.GetAsync($"/Shop/Detail/{System.Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
