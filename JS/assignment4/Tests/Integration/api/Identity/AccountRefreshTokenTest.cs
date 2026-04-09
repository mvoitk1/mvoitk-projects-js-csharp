using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using com.akaver.Extensions;
using Microsoft.AspNetCore.Mvc.Testing;
using PublicApi.DTO.v1;
using PublicApi.DTO.v1.Identity;
using Tests.Helpers;
using Tests.Integration;
using WebApp;

namespace App.Test.Integration.api.Identity;

[Collection("NonParallel")]
public class AccountRefreshTokenTest : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory<Program> _factory;

    // These must match the values in appsettings.json
    private const string JwtKey =
        "kadfj,hvd_fkw3  894tASBVC6e786tdfjnbjkd..fbnkadfj,hvd_fkw3  894tASBVC6e786tdfjnbjkd..fbnkadfj,hvd_fkw3  894tASBVC6e786tdfjnbjkd..fbn";
    private const string JwtIssuer = "taltech.akaver.com";

    public AccountRefreshTokenTest(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task SingleRefreshCycle_ReturnsNewTokens()
    {
        // Arrange
        var loginData = await ApiTestBase.LoginAsync(_client, "akaver@akaver.com", "Foo.bar1", expiresInSeconds: 1);
        await Task.Delay(1500);

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1.0/Account/RefreshToken?expiresInSeconds=5",
            new { Jwt = loginData.Token, RefreshToken = loginData.RefreshToken });
        var contentStr = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var refreshData = JsonSerializer.Deserialize<JwtResponse>(contentStr, JsonHelper.CamelCase);
        Assert.NotNull(refreshData);
        Assert.NotEqual(loginData.Token, refreshData.Token);
        Assert.NotEqual(loginData.RefreshToken, refreshData.RefreshToken);
    }

    [Fact]
    public async Task MultiCycleRefresh_ThreeConsecutiveCycles()
    {
        // Arrange - Login
        var data0 = await ApiTestBase.LoginAsync(_client, "akaver@akaver.com", "Foo.bar1", expiresInSeconds: 1);
        var tokens = new List<(string Jwt, string Rt)> { (data0.Token, data0.RefreshToken) };

        // Act - 3 refresh cycles
        for (var i = 0; i < 3; i++)
        {
            await Task.Delay(1500);
            var response = await _client.PostAsJsonAsync("/api/v1.0/Account/RefreshToken?expiresInSeconds=1",
                new { Jwt = tokens[^1].Jwt, RefreshToken = tokens[^1].Rt });
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var contentStr = await response.Content.ReadAsStringAsync();
            var refreshData = JsonSerializer.Deserialize<JwtResponse>(contentStr, JsonHelper.CamelCase);
            Assert.NotNull(refreshData);
            tokens.Add((refreshData.Token, refreshData.RefreshToken));
        }

        // Assert - all 4 JWTs distinct, all 4 RTs distinct
        Assert.Equal(4, tokens.Select(t => t.Jwt).Distinct().Count());
        Assert.Equal(4, tokens.Select(t => t.Rt).Distinct().Count());
    }

    [Fact]
    public async Task PreviousTokenGracePeriod_OldTokenAcceptedImmediately()
    {
        // Arrange
        var loginData = await ApiTestBase.LoginAsync(_client, "akaver@akaver.com", "Foo.bar1", expiresInSeconds: 1);
        await Task.Delay(1500);

        // First refresh
        var response1 = await _client.PostAsJsonAsync("/api/v1.0/Account/RefreshToken?expiresInSeconds=5",
            new { Jwt = loginData.Token, RefreshToken = loginData.RefreshToken });
        Assert.Equal(HttpStatusCode.OK, response1.StatusCode);
        var refreshData1 = JsonSerializer.Deserialize<JwtResponse>(
            await response1.Content.ReadAsStringAsync(), JsonHelper.CamelCase);
        Assert.NotNull(refreshData1);

        // Act - immediately refresh again using the OLD RT₁ with the new JWT₂
        var response2 = await _client.PostAsJsonAsync("/api/v1.0/Account/RefreshToken?expiresInSeconds=5",
            new { Jwt = refreshData1.Token, RefreshToken = loginData.RefreshToken });

        // Assert
        Assert.Equal(HttpStatusCode.OK, response2.StatusCode);
    }

    [Fact]
    public async Task InvalidRefreshToken_ReturnsError()
    {
        // Arrange
        var loginData = await ApiTestBase.LoginAsync(_client, "akaver@akaver.com", "Foo.bar1");

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1.0/Account/RefreshToken",
            new { Jwt = loginData.Token, RefreshToken = Guid.NewGuid().ToString() });

        // Assert
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        var contentStr = await response.Content.ReadAsStringAsync();
        Assert.Contains("no valid refresh tokens found", contentStr);
    }

    [Fact]
    public async Task MalformedJwt_Returns400()
    {
        // Act
        var response = await _client.PostAsJsonAsync("/api/v1.0/Account/RefreshToken",
            new { Jwt = "not-a-jwt", RefreshToken = Guid.NewGuid().ToString() });

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var contentStr = await response.Content.ReadAsStringAsync();
        Assert.Contains("Cant parse the token", contentStr);
    }

    [Fact]
    public async Task JwtMissingEmailClaim_Returns400()
    {
        // Arrange - craft JWT with empty claims using the app's key/issuer
        var jwt = IdentityExtensions.GenerateJwt(
            new List<Claim>(),
            JwtKey,
            JwtIssuer,
            JwtIssuer,
            DateTime.UtcNow.AddMinutes(5)
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1.0/Account/RefreshToken",
            new { Jwt = jwt, RefreshToken = Guid.NewGuid().ToString() });

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var contentStr = await response.Content.ReadAsStringAsync();
        Assert.Contains("No email in jwt", contentStr);
    }

    [Fact]
    public async Task JwtContentConsistencyAcrossRefresh()
    {
        // Arrange
        var loginData = await ApiTestBase.LoginAsync(_client, "akaver@akaver.com", "Foo.bar1", expiresInSeconds: 1);
        var jwt1 = new JwtSecurityTokenHandler().ReadJwtToken(loginData.Token);
        await Task.Delay(1500);

        // Act - refresh
        var response = await _client.PostAsJsonAsync("/api/v1.0/Account/RefreshToken?expiresInSeconds=5",
            new { Jwt = loginData.Token, RefreshToken = loginData.RefreshToken });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var refreshData = JsonSerializer.Deserialize<JwtResponse>(
            await response.Content.ReadAsStringAsync(), JsonHelper.CamelCase);
        Assert.NotNull(refreshData);
        var jwt2 = new JwtSecurityTokenHandler().ReadJwtToken(refreshData.Token);

        // Assert - stable claims
        Assert.Equal(
            jwt1.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value,
            jwt2.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value);
        Assert.Equal(
            jwt1.Claims.First(c => c.Type == ClaimTypes.Email).Value,
            jwt2.Claims.First(c => c.Type == ClaimTypes.Email).Value);
        Assert.Equal(jwt1.Issuer, jwt2.Issuer);
        Assert.Equal(jwt1.Audiences.First(), jwt2.Audiences.First());

        // Assert - only exp/nbf/iat differ
        var changingTypes = new HashSet<string> { "exp", "nbf", "iat" };
        var stableClaims1 = jwt1.Claims.Where(c => !changingTypes.Contains(c.Type)).OrderBy(c => c.Type).ToList();
        var stableClaims2 = jwt2.Claims.Where(c => !changingTypes.Contains(c.Type)).OrderBy(c => c.Type).ToList();
        Assert.Equal(stableClaims1.Count, stableClaims2.Count);
        for (var i = 0; i < stableClaims1.Count; i++)
        {
            Assert.Equal(stableClaims1[i].Type, stableClaims2[i].Type);
            Assert.Equal(stableClaims1[i].Value, stableClaims2[i].Value);
        }

        // Assert - no duplicate claim types in refreshed JWT
        var claimGroups = jwt2.Claims.GroupBy(c => c.Type);
        foreach (var group in claimGroups)
        {
            Assert.True(group.Count() == 1,
                $"Duplicate claim type in refreshed JWT: {group.Key} (count: {group.Count()})");
        }
    }

    [Fact]
    public async Task NoDoubleRecords_FiveCyclesSucceed()
    {
        // Arrange - register a fresh user
        var email = $"test-{Guid.NewGuid():N}@test.com";
        var data = await ApiTestBase.RegisterAsync(_client, email, "Test.pass1", "Five", "Cycles",
            expiresInSeconds: 1);
        var allRts = new List<string> { data.RefreshToken };

        // Act - 5 refresh cycles
        var currentJwt = data.Token;
        var currentRt = data.RefreshToken;
        for (var i = 0; i < 5; i++)
        {
            await Task.Delay(1500);
            var response = await _client.PostAsJsonAsync("/api/v1.0/Account/RefreshToken?expiresInSeconds=1",
                new { Jwt = currentJwt, RefreshToken = currentRt });
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var refreshData = JsonSerializer.Deserialize<JwtResponse>(
                await response.Content.ReadAsStringAsync(), JsonHelper.CamelCase);
            Assert.NotNull(refreshData);
            currentJwt = refreshData.Token;
            currentRt = refreshData.RefreshToken;
            allRts.Add(currentRt);
        }

        // Assert - all 6 refresh tokens are distinct
        Assert.Equal(6, allRts.Distinct().Count());
    }

    [Fact]
    public async Task ConcurrentSessions_IndependentRefresh()
    {
        // Arrange - login twice (two sessions)
        var loginA = await ApiTestBase.LoginAsync(_client, "akaver@akaver.com", "Foo.bar1", expiresInSeconds: 1);
        var loginB = await ApiTestBase.LoginAsync(_client, "akaver@akaver.com", "Foo.bar1", expiresInSeconds: 1);

        await Task.Delay(1500);

        // Act - refresh session A
        var responseA = await _client.PostAsJsonAsync("/api/v1.0/Account/RefreshToken?expiresInSeconds=5",
            new { Jwt = loginA.Token, RefreshToken = loginA.RefreshToken });

        // Refresh session B
        var responseB = await _client.PostAsJsonAsync("/api/v1.0/Account/RefreshToken?expiresInSeconds=5",
            new { Jwt = loginB.Token, RefreshToken = loginB.RefreshToken });

        // Assert
        Assert.Equal(HttpStatusCode.OK, responseA.StatusCode);
        Assert.Equal(HttpStatusCode.OK, responseB.StatusCode);
    }
}
