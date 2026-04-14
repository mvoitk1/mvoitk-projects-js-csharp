using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using PublicApi.DTO.v1.Identity;
using Tests.Helpers;
using Tests.Integration;
using WebApp;
using Xunit.Abstractions;

namespace App.Test.Integration.api;

[Collection("NonParallel")]
public class TokenFlow: IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory<Program> _factory;
    private readonly ITestOutputHelper _testOutputHelper;

    public TokenFlow(CustomWebApplicationFactory<Program> factory, ITestOutputHelper testOutputHelper)
    {
        _factory = factory;
        _testOutputHelper = testOutputHelper;
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task JwtRefreshTokenTest()
    {
        var user = "akaver@akaver.com";
        var pass = "Foo.bar1";
        
        // get jwt
        var response =
            await _client.PostAsJsonAsync("/api/v1.0/Account/Login?expiresInSeconds=1", new {email = user, password = pass});
        var contentStr = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();

        var loginData = JsonSerializer.Deserialize<JwtResponse>(contentStr, JsonHelper.CamelCase);

        Assert.NotNull(loginData);
        Assert.NotNull(loginData.Token);
        Assert.True(loginData.Token.Length > 0);

        Assert.NotNull(loginData.RefreshToken);
        Assert.True(loginData.RefreshToken.Length > 0);

        await Task.Delay(1500);
        
        // try to refresh
        
        response =
            await _client.PostAsJsonAsync("/api/v1.0/Account/RefreshToken?expiresInSeconds=5", 
                new
                {
                    Jwt = loginData.Token,
                    RefreshToken = loginData.RefreshToken
                });

        contentStr = await response.Content.ReadAsStringAsync();

        response.EnsureSuccessStatusCode();
        loginData = JsonSerializer.Deserialize<JwtResponse>(contentStr, JsonHelper.CamelCase);
        
        var jwtToken = new JwtSecurityTokenHandler().ReadJwtToken(loginData!.Token);
        _testOutputHelper.WriteLine(DateTime.UtcNow.ToUniversalTime().ToLongTimeString());
        _testOutputHelper.WriteLine(jwtToken.ValidTo.ToLongTimeString());

    }
}