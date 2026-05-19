using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Tests.Shared;
using Xunit;

namespace WebApp.Tests.Integration.Views;

[Collection("Database tests")]
public class CartViewTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public CartViewTests(CustomWebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task Cart_AnonymousUser_RedirectsToLogin()
    {
        var response = await _client.GetAsync("/Cart");

        // [Authorize] on CartController redirects unauthenticated cookie users.
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        Assert.Contains("Login", response.Headers.Location!.ToString(),
            System.StringComparison.OrdinalIgnoreCase);
    }
}
