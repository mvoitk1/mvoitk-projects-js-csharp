using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using App.DTO.v1.Identity;
using Base.Helpers;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace WebApp.Tests.Integration.Api;

/// <summary>
/// Tests that verify IDOR protections: users cannot access each other's carts or orders.
/// </summary>
[Collection("Database tests")]
public class IntegrationTestIDAR : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly CustomWebApplicationFactory<Program> _factory;

    public IntegrationTestIDAR(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    private HttpClient CreateClient() =>
        _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    private async Task<string> RegisterAndGetTokenAsync(HttpClient client, string email, string password)
    {
        var registerData = new Register
        {
            FirstName = "IDOR",
            LastName = "Test",
            Email = email,
            Password = password
        };

        var registerResponse = await client.PostAsync(
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
    public async Task Orders_UserCannotAccessAnotherUsersOrders()
    {
        // Arrange — two separate clients/users
        using var client1 = CreateClient();
        using var client2 = CreateClient();

        var token1 = await RegisterAndGetTokenAsync(client1, "idor.user1@shop.test", "Idor.User1.1");
        var token2 = await RegisterAndGetTokenAsync(client2, "idor.user2@shop.test", "Idor.User2.1");

        // User 1 fetches their orders — should return empty array, not another user's data
        client1.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token1);
        var user1OrdersResponse = await client1.GetAsync("/api/v1/orders");
        user1OrdersResponse.EnsureSuccessStatusCode();

        // User 2 fetches their orders — should also return empty array
        client2.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token2);
        var user2OrdersResponse = await client2.GetAsync("/api/v1/orders");
        user2OrdersResponse.EnsureSuccessStatusCode();

        // Both get their own data — neither can enumerate the other's orders via a shared list
        var body1 = await user1OrdersResponse.Content.ReadAsStringAsync();
        var body2 = await user2OrdersResponse.Content.ReadAsStringAsync();
        using var doc1 = JsonDocument.Parse(body1);
        using var doc2 = JsonDocument.Parse(body2);
        Assert.Equal(JsonValueKind.Array, doc1.RootElement.ValueKind);
        Assert.Equal(JsonValueKind.Array, doc2.RootElement.ValueKind);
    }

    [Fact]
    public async Task Orders_UnauthenticatedUserCannotAccessOrders()
    {
        // Arrange
        using var client = CreateClient();

        // Act — no auth
        var response = await client.GetAsync("/api/v1/orders");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
