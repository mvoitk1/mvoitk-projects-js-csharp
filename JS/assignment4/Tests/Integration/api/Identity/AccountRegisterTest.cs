using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using PublicApi.DTO.v1;
using PublicApi.DTO.v1.Identity;
using Tests.Helpers;
using Tests.Integration;
using WebApp;

namespace App.Test.Integration.api.Identity;

[Collection("NonParallel")]
public class AccountRegisterTest : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory<Program> _factory;

    public AccountRegisterTest(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task RegisterSuccess_ReturnsJwtWithNameClaims()
    {
        // Arrange
        var email = $"test-{Guid.NewGuid():N}@test.com";

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1.0/Account/Register",
            new { email, password = "Test.pass1", firstName = "John", lastName = "Doe" });
        var contentStr = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var registerData = JsonSerializer.Deserialize<JwtResponse>(contentStr, JsonHelper.CamelCase);
        Assert.NotNull(registerData);
        Assert.NotEmpty(registerData.Token);
        Assert.Equal(36, registerData.RefreshToken.Length);
        Assert.Equal("John", registerData.FirstName);
        Assert.Equal("Doe", registerData.LastName);

        // Verify JWT claims
        var jwtToken = new JwtSecurityTokenHandler().ReadJwtToken(registerData.Token);
        var givenName = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.GivenName);
        var surname = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Surname);
        Assert.NotNull(givenName);
        Assert.Equal("John", givenName.Value);
        Assert.NotNull(surname);
        Assert.Equal("Doe", surname.Value);
    }

    [Fact]
    public async Task RegisterDuplicateEmail_Returns400()
    {
        // Arrange
        var email = $"test-{Guid.NewGuid():N}@test.com";
        await _client.PostAsJsonAsync("/api/v1.0/Account/Register",
            new { email, password = "Test.pass1", firstName = "First", lastName = "User" });

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1.0/Account/Register",
            new { email, password = "Test.pass1", firstName = "Second", lastName = "User" });
        var contentStr = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var message = JsonSerializer.Deserialize<Message>(contentStr, JsonHelper.CamelCase);
        Assert.NotNull(message);
        Assert.Contains("User already registered", message.Messages);
    }

    [Fact]
    public async Task RegisterWeakPassword_Returns400WithErrors()
    {
        // Act
        var email = $"test-{Guid.NewGuid():N}@test.com";
        var response = await _client.PostAsJsonAsync("/api/v1.0/Account/Register",
            new { email, password = "123", firstName = "Weak", lastName = "Pass" });
        var contentStr = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var message = JsonSerializer.Deserialize<Message>(contentStr, JsonHelper.CamelCase);
        Assert.NotNull(message);
        Assert.True(message.Messages.Count > 0, "Expected Identity validation error messages");
    }

    [Fact]
    public async Task RegisterThenLogin_Succeeds()
    {
        // Arrange
        var email = $"test-{Guid.NewGuid():N}@test.com";
        var password = "Test.pass1";
        await ApiTestBase.RegisterAsync(_client, email, password, "Reg", "Login");

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1.0/Account/Login",
            new { email, password });

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var contentStr = await response.Content.ReadAsStringAsync();
        var loginData = JsonSerializer.Deserialize<JwtResponse>(contentStr, JsonHelper.CamelCase);
        Assert.NotNull(loginData);
        Assert.NotEmpty(loginData.Token);
    }

    [Fact]
    public async Task RegisterJwt_NoDuplicateClaims()
    {
        // Arrange
        var email = $"test-{Guid.NewGuid():N}@test.com";

        // Act
        var registerData =
            await ApiTestBase.RegisterAsync(_client, email, "Test.pass1", "NoDup", "Claims");
        var jwtToken = new JwtSecurityTokenHandler().ReadJwtToken(registerData.Token);

        // Assert - no duplicate claim types
        var claimGroups = jwtToken.Claims.GroupBy(c => c.Type);
        foreach (var group in claimGroups)
        {
            Assert.True(group.Count() == 1, $"Duplicate claim type found: {group.Key} (count: {group.Count()})");
        }

        // Specifically check key claim types exist
        Assert.Single(jwtToken.Claims, c => c.Type == ClaimTypes.Email);
        Assert.Single(jwtToken.Claims, c => c.Type == ClaimTypes.NameIdentifier);
        Assert.Single(jwtToken.Claims, c => c.Type == ClaimTypes.GivenName);
        Assert.Single(jwtToken.Claims, c => c.Type == ClaimTypes.Surname);
    }
}
