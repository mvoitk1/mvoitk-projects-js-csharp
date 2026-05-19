using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Tests.Shared;
using Xunit;

namespace WebApp.Tests.Integration.Views;

[Collection("Database tests")]
public class AdminProductsViewTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public AdminProductsViewTests(CustomWebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task AdminProducts_AnonymousUser_RedirectsToLogin()
    {
        // AdminBaseController has [Authorize(Roles = "Admin")] — unauthenticated
        // cookie users hit the configured login challenge.
        var response = await _client.GetAsync("/Admin/Products");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        Assert.Contains("Login", response.Headers.Location!.ToString(),
            System.StringComparison.OrdinalIgnoreCase);
    }
}
