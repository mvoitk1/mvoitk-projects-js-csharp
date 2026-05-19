using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Catalog.Web.Dtos.v1.Products;
using Base.Helpers;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace WebApp.Tests.Integration.Api;

[Collection("Database tests")]
public class IntegrationTestDecimalPrecision : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public IntegrationTestDecimalPrecision(CustomWebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task ProductVariantPrice_RoundTripsExactDecimal()
    {
        // The seeded variant prices are 29.99 — assert the API serializes the value
        // exactly, with no float-imprecision tail (e.g. 29.989999...).
        var listResponse = await _client.GetAsync("/api/v1/products");
        listResponse.EnsureSuccessStatusCode();
        var listBody = await listResponse.Content.ReadAsStringAsync();

        var items = JsonSerializer.Deserialize<System.Collections.Generic.List<ProductListItemDto>>(
            listBody, JsonHelpers.JsonSerializerOptionsCamelCase)!;
        Assert.NotEmpty(items);

        var detailResponse = await _client.GetAsync($"/api/v1/products/{items.First().Id}");
        detailResponse.EnsureSuccessStatusCode();
        var detailBody = await detailResponse.Content.ReadAsStringAsync();

        // 1) Raw JSON must contain "29.99" with no extra digits (would fail for double precision drift).
        Assert.Contains("\"price\": 29.99", detailBody);
        Assert.DoesNotContain("29.989999", detailBody);
        Assert.DoesNotContain("29.990000000", detailBody);

        // 2) Parsed back as decimal — equal to the literal seed value.
        var product = JsonSerializer.Deserialize<ProductDto>(
            detailBody, JsonHelpers.JsonSerializerOptionsCamelCase)!;
        Assert.NotEmpty(product.Variants);
        Assert.All(product.Variants, v => Assert.Equal(29.99m, v.Price));
    }
}
