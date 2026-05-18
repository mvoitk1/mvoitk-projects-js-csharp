using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace WebApp.Tests.Integration.Contract;

[Collection("Database tests")]
public class SwaggerTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public SwaggerTests(CustomWebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    private async Task<JsonDocument> GetSwaggerAsync()
    {
        var response = await _client.GetAsync("/swagger/v1/swagger.json");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.False(string.IsNullOrWhiteSpace(body));
        return JsonDocument.Parse(body);
    }

    [Fact]
    public async Task SwaggerJson_ReturnsValidOpenApiDocument()
    {
        using var doc = await GetSwaggerAsync();

        Assert.True(doc.RootElement.TryGetProperty("openapi", out _));
        Assert.True(doc.RootElement.TryGetProperty("paths", out var paths));
        Assert.Equal(JsonValueKind.Object, paths.ValueKind);
        Assert.True(paths.EnumerateObject().Any());
    }

    [Theory]
    [InlineData("/api/v1/Products")]
    [InlineData("/api/v1/Categories")]
    [InlineData("/api/v1/Collections")]
    [InlineData("/api/v1/Cart")]
    [InlineData("/api/v1/Orders")]
    public async Task SwaggerJson_ContainsExpectedRoutes(string path)
    {
        using var doc = await GetSwaggerAsync();
        var paths = doc.RootElement.GetProperty("paths");

        var pathNames = paths.EnumerateObject().Select(p => p.Name).ToList();
        Assert.Contains(pathNames, name =>
            name.Equals(path, System.StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task SwaggerJson_SchemasDoNotIncludeDomainEntities()
    {
        using var doc = await GetSwaggerAsync();

        if (!doc.RootElement.TryGetProperty("components", out var components)) return;
        if (!components.TryGetProperty("schemas", out var schemas)) return;

        var schemaNames = schemas.EnumerateObject().Select(p => p.Name).ToList();

        // Domain types that must not leak into the public API contract.
        var forbidden = new[]
        {
            "Product", "Category", "Collection", "Color", "Size",
            "ProductImage", "ProductVariant", "Cart", "CartItem",
            "Order", "OrderItem", "ProductCategory", "AppUser", "AppRole"
        };

        foreach (var bad in forbidden)
        {
            Assert.DoesNotContain(schemaNames, name =>
                name.Equals(bad, System.StringComparison.OrdinalIgnoreCase));
        }
    }
}
