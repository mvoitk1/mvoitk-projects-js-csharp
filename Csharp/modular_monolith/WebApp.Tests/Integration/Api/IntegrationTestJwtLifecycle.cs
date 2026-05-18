using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Users.Application.Dtos.Identity;
using Base.Helpers;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace WebApp.Tests.Integration.Api;

[Collection("Database tests")]
public class IntegrationTestJwtLifecycle : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public IntegrationTestJwtLifecycle(CustomWebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    private static StringContent JsonContent<T>(T value) =>
        new(JsonSerializer.Serialize(value, JsonHelpers.JsonSerializerOptionsCamelCase),
            Encoding.UTF8, "application/json");

    private async Task<JWTResponse> RegisterAsync(string email, int? jwtSeconds = null)
    {
        var url = "/api/v1/account/register"
            + (jwtSeconds.HasValue ? $"?jwtExpiresInSeconds={jwtSeconds.Value}" : "");
        var response = await _client.PostAsync(url, JsonContent(new Register
        {
            FirstName = "Test",
            LastName = "User",
            Email = email,
            Password = "Test.Password.1"
        }));
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<JWTResponse>(body, JsonHelpers.JsonSerializerOptionsCamelCase)!;
    }

    [Fact]
    public async Task ExpiredJwt_OnProtectedEndpoint_Returns401()
    {
        var tokens = await RegisterAsync("jwt.expiry@shop.test", jwtSeconds: 1);

        // Wait for the access token to expire.
        await Task.Delay(2_500);

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/cart");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokens.JWT);
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task RefreshToken_RotatesAndRejectsUnknownToken()
    {
        var tokens = await RegisterAsync("jwt.refresh@shop.test");

        var renew = await _client.PostAsync(
            "/api/v1/account/RenewRefreshToken",
            JsonContent(new RefreshTokenModel { Jwt = tokens.JWT, RefreshToken = tokens.RefreshToken }));
        renew.EnsureSuccessStatusCode();
        var renewed = JsonSerializer.Deserialize<JWTResponse>(
            await renew.Content.ReadAsStringAsync(),
            JsonHelpers.JsonSerializerOptionsCamelCase)!;

        Assert.NotEqual(tokens.RefreshToken, renewed.RefreshToken);

        // A garbage refresh token is rejected (returns 500 / non-OK via IdentityService NoValidToken path).
        var bogus = await _client.PostAsync(
            "/api/v1/account/RenewRefreshToken",
            JsonContent(new RefreshTokenModel
            {
                Jwt = tokens.JWT,
                RefreshToken = System.Guid.NewGuid().ToString()
            }));

        Assert.NotEqual(HttpStatusCode.OK, bogus.StatusCode);
    }
}
