using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace WebApp.Tests.Integration.Views;

[Collection("Database tests")]
public class CheckoutViewTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public CheckoutViewTests(CustomWebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task Checkout_AnonymousUser_RedirectsToLogin()
    {
        var response = await _client.GetAsync("/Checkout");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        Assert.Contains("Login", response.Headers.Location!.ToString(),
            System.StringComparison.OrdinalIgnoreCase);
    }
}
