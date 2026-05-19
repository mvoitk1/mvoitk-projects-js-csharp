using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Tests.Shared;
using Xunit;

namespace WebApp.Tests.Integration.Localization;

[Collection("Database tests")]
public class ViewLocalizationTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly CustomWebApplicationFactory<Program> _factory;

    public ViewLocalizationTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    private HttpClient CreateClient() =>
        _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

    [Fact]
    public async Task Home_EnglishCulture_ContainsEnglishStrings()
    {
        var client = CreateClient();

        var response = await client.GetAsync("/?culture=en&ui-culture=en");
        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync();

        Assert.Contains("New Season, New Style", html);
        Assert.Contains("Shop Now", html);
    }

    [Fact]
    public async Task Home_EstonianCulture_ContainsEstonianStrings()
    {
        var client = CreateClient();

        var response = await client.GetAsync("/?culture=et&ui-culture=et");
        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync();

        // Razor HTML-encodes non-ASCII characters, so we assert on ASCII-safe slices
        // of the Estonian strings. "Uus Hooaeg, Uus Stiil" has no special chars.
        Assert.Contains("Uus Hooaeg, Uus Stiil", html);
        Assert.Contains("Kollektsioonid", html);
        Assert.DoesNotContain("New Season, New Style", html);
    }

    [Fact]
    public async Task Home_QueryStringCultureOverridesHeader()
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.AcceptLanguage.Clear();
        client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("en");

        // QueryStringRequestCultureProvider is registered first, so ?culture=et wins.
        var response = await client.GetAsync("/?culture=et");
        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync();

        Assert.Contains("Uus Hooaeg, Uus Stiil", html);
    }
}
