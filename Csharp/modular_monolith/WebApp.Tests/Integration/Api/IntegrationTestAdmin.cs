using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Tests.Shared.Helpers;
using Tests.Shared;
using Xunit;

namespace WebApp.Tests.Integration.Api;

/// <summary>
/// Verifies that the <c>Admin</c>-role back-office endpoints reject anonymous callers (401)
/// and authenticated non-admin customers (403).
/// </summary>
[Collection("Database tests")]
public class IntegrationTestAdmin : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly CustomWebApplicationFactory<Program> _factory;

    public IntegrationTestAdmin(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    private HttpClient CreateClient() =>
        _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    [Theory]
    [InlineData("/api/v1/admin/products")]
    [InlineData("/api/v1/admin/categories")]
    [InlineData("/api/v1/admin/orders")]
    public async Task AdminEndpoint_AnonymousCaller_Returns401(string url)
    {
        using var client = CreateClient();

        var response = await client.GetAsync(url);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("/api/v1/admin/products")]
    [InlineData("/api/v1/admin/categories")]
    [InlineData("/api/v1/admin/orders")]
    public async Task AdminEndpoint_AuthenticatedNonAdmin_Returns403(string url)
    {
        using var client = CreateClient();
        var jwt = await IdentityHelper.SetupUserAsync(
            client, "Plain", "Customer", "Plain.Customer.1", $"nonadmin{url.GetHashCode():X}@shop.test");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt.JWT);

        var response = await client.GetAsync(url);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
