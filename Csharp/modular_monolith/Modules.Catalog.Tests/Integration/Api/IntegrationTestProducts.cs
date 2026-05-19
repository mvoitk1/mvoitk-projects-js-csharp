using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Users.Application.Dtos.Identity;
using Base.Helpers;
using Microsoft.AspNetCore.Mvc.Testing;
using Tests.Shared;
using Xunit;

namespace Tests.Integration.Api;

[Collection("Database tests")]
public class IntegrationTestProducts : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory<Program> _factory;

    public IntegrationTestProducts(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task GetProducts_ReturnsOk()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/products");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.NotNull(body);
    }

    [Fact]
    public async Task GetProducts_ReturnsJsonArray()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/products");
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
    }

    private async Task<string> RegisterAndGetTokenAsync(string email, string password)
    {
        var registerData = new Register
        {
            FirstName = "Test",
            LastName = "User",
            Email = email,
            Password = password
        };

        var registerResponse = await _client.PostAsync(
            "/api/v1/account/register",
            new StringContent(
                JsonSerializer.Serialize(registerData, JsonHelpers.JsonSerializerOptionsCamelCase),
                Encoding.UTF8, "application/json"));

        registerResponse.EnsureSuccessStatusCode();
        var body = await registerResponse.Content.ReadAsStringAsync();
        var jwt = JsonSerializer.Deserialize<JWTResponse>(body, JsonHelpers.JsonSerializerOptionsCamelCase);
        return jwt!.JWT;
    }

    [Fact]
    public async Task GetCart_RequiresAuthentication()
    {
        // Act — no auth header
        var response = await _client.GetAsync("/api/v1/cart");

        // Assert — should be 401
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetCart_AuthenticatedUser_ReturnsOk()
    {
        // Arrange
        var token = await RegisterAndGetTokenAsync("cart.test@shop.test", "Cart.Test.1");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/v1/cart");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Cleanup
        _client.DefaultRequestHeaders.Authorization = null;
    }
}
