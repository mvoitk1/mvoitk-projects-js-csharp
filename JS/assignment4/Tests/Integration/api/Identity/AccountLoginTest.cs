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
public class AccountLoginTest : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory<Program> _factory;

    public AccountLoginTest(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task LoginSuccess_ReturnsJwtResponse()
    {
        // Act
        var response = await _client.PostAsJsonAsync("/api/v1.0/Account/Login",
            new { email = "akaver@akaver.com", password = "Foo.bar1" });
        var contentStr = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var loginData = JsonSerializer.Deserialize<JwtResponse>(contentStr, JsonHelper.CamelCase);
        Assert.NotNull(loginData);
        Assert.NotEmpty(loginData.Token);
        Assert.Equal(36, loginData.RefreshToken.Length);
        Assert.Equal("Andres", loginData.FirstName);
        Assert.Equal("Käver", loginData.LastName);
    }

    [Fact]
    public async Task LoginWrongPassword_Returns404WithGenericError()
    {
        // Act
        var response = await _client.PostAsJsonAsync("/api/v1.0/Account/Login",
            new { email = "akaver@akaver.com", password = "WrongPassword1" });
        var contentStr = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var message = JsonSerializer.Deserialize<Message>(contentStr, JsonHelper.CamelCase);
        Assert.NotNull(message);
        Assert.Contains("User/Password problem!", message.Messages);
    }

    [Fact]
    public async Task LoginNonExistentEmail_Returns404WithGenericError()
    {
        // Act
        var response = await _client.PostAsJsonAsync("/api/v1.0/Account/Login",
            new { email = "nonexistent@test.com", password = "Foo.bar1" });
        var contentStr = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var message = JsonSerializer.Deserialize<Message>(contentStr, JsonHelper.CamelCase);
        Assert.NotNull(message);
        Assert.Contains("User/Password problem!", message.Messages);
    }

    [Fact]
    public async Task LoginJwtContent_ContainsCorrectClaims()
    {
        // Act
        var loginData = await ApiTestBase.LoginAsync(_client, "akaver@akaver.com", "Foo.bar1");
        var jwtToken = new JwtSecurityTokenHandler().ReadJwtToken(loginData.Token);

        // Assert
        var emailClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);
        Assert.NotNull(emailClaim);
        Assert.Equal("akaver@akaver.com", emailClaim.Value);

        Assert.Equal("taltech.akaver.com", jwtToken.Issuer);
        Assert.Contains("taltech.akaver.com", jwtToken.Audiences);

        // Seeded user does not have GivenName claim added via AddClaimAsync
        var givenNameClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.GivenName);
        Assert.Null(givenNameClaim);
    }

    [Fact]
    public async Task LoginCustomExpiration_JwtExpiresAtRequestedTime()
    {
        // Act
        var before = DateTime.UtcNow;
        var loginData = await ApiTestBase.LoginAsync(_client, "akaver@akaver.com", "Foo.bar1", expiresInSeconds: 3);
        var jwtToken = new JwtSecurityTokenHandler().ReadJwtToken(loginData.Token);

        // Assert
        var expectedExpiration = before.AddSeconds(3);
        var actualExpiration = jwtToken.ValidTo;
        var diff = Math.Abs((actualExpiration - expectedExpiration).TotalSeconds);
        Assert.True(diff < 2, $"JWT expiration {actualExpiration} is not within 2s of expected {expectedExpiration}");
    }

    [Fact]
    public async Task MultipleLogins_EachProducesDistinctRefreshToken()
    {
        // Arrange - login three times
        var login1 = await ApiTestBase.LoginAsync(_client, "akaver@akaver.com", "Foo.bar1");
        var login2 = await ApiTestBase.LoginAsync(_client, "akaver@akaver.com", "Foo.bar1");
        var login3 = await ApiTestBase.LoginAsync(_client, "akaver@akaver.com", "Foo.bar1");

        // Assert - all refresh tokens are distinct
        var tokens = new[] { login1.RefreshToken, login2.RefreshToken, login3.RefreshToken };
        Assert.Equal(tokens.Length, tokens.Distinct().Count());

        // Each refresh token can be used for a valid refresh
        foreach (var login in new[] { login1, login2, login3 })
        {
            var refreshResponse = await _client.PostAsJsonAsync("/api/v1.0/Account/RefreshToken?expiresInSeconds=5",
                new { Jwt = login.Token, RefreshToken = login.RefreshToken });
            Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);
        }
    }
}
