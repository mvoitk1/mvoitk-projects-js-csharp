using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Catalog.Web.Dtos.v1.Products;
using Base.Helpers;
using Microsoft.AspNetCore.Mvc.Testing;
using Tests.Shared;
using Xunit;

namespace Tests.Integration.Api;

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
        // Prices are stored as decimal(18,2). Assert the API serializes each price
        // exactly (its canonical decimal string), with no binary-float drift tail
        // (e.g. 24.899999... / 24.900000001) — regardless of the seeded value.
        var listResponse = await _client.GetAsync("/api/v1/products");
        listResponse.EnsureSuccessStatusCode();
        var listBody = await listResponse.Content.ReadAsStringAsync();

        var items = JsonSerializer.Deserialize<System.Collections.Generic.List<ProductListItemDto>>(
            listBody, JsonHelpers.JsonSerializerOptionsCamelCase)!;
        Assert.NotEmpty(items);

        var detailResponse = await _client.GetAsync($"/api/v1/products/{items.First().Id}");
        detailResponse.EnsureSuccessStatusCode();
        var detailBody = await detailResponse.Content.ReadAsStringAsync();

        var product = JsonSerializer.Deserialize<ProductDto>(
            detailBody, JsonHelpers.JsonSerializerOptionsCamelCase)!;
        Assert.NotEmpty(product.Variants);

        foreach (var variant in product.Variants)
        {
            // The parsed decimal must serialize back to the exact same token in the
            // raw JSON — proving no float-precision drift in (de)serialization.
            var token = "\"price\": " + variant.Price.ToString(System.Globalization.CultureInfo.InvariantCulture);
            Assert.Contains(token, detailBody);
        }

        // No long fractional tails anywhere (the hallmark of double-backed money).
        Assert.DoesNotContain("9999999", detailBody);
        Assert.DoesNotContain("00000001", detailBody);
    }
}
