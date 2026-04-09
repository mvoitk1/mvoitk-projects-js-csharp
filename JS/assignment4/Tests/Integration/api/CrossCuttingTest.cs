using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using PublicApi.DTO.v1.Todo;
using Tests.Helpers;
using Tests.Integration;
using WebApp;

namespace App.Test.Integration.api;

[Collection("NonParallel")]
public class CrossCuttingTest : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory<Program> _factory;

    public CrossCuttingTest(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task ExpiredJwt_Returns401()
    {
        // Arrange
        var loginData = await ApiTestBase.LoginAsync(_client, "akaver@akaver.com", "Foo.bar1", expiresInSeconds: 1);
        await Task.Delay(2000);

        // Act
        var msg = ApiTestBase.CreateAuthMessage(HttpMethod.Get, "/api/v1.0/TodoCategories", loginData.Token);
        var response = await _client.SendAsync(msg);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CrossUserDataAccess_Returns404()
    {
        // User A registers and creates a category
        var emailA = $"test-{Guid.NewGuid():N}@test.com";
        var jwtA = await ApiTestBase.RegisterAsync(_client, emailA, "Test.pass1", "Cross", "UserA");

        var category = TestDataFactory.CreateTodoCategory("CrossCat");
        var createMsg = ApiTestBase.CreateAuthMessage(HttpMethod.Post, "/api/v1.0/TodoCategories", jwtA.Token);
        createMsg.Content = JsonContent.Create(category);
        var createResponse = await _client.SendAsync(createMsg);
        var created = JsonSerializer.Deserialize<TodoCategory>(
            await createResponse.Content.ReadAsStringAsync(), JsonHelper.CamelCase);
        Assert.NotNull(created);

        // User B registers and tries to GET User A's category
        var emailB = $"test-{Guid.NewGuid():N}@test.com";
        var jwtB = await ApiTestBase.RegisterAsync(_client, emailB, "Test.pass1", "Cross", "UserB");

        var getMsg = ApiTestBase.CreateAuthMessage(HttpMethod.Get,
            $"/api/v1.0/TodoCategories/{created.Id}", jwtB.Token);
        var getResponse = await _client.SendAsync(getMsg);

        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task ApiVersioning_V1Returns200_V2ReturnsError()
    {
        // Arrange
        var loginData = await ApiTestBase.LoginAsync(_client, "akaver@akaver.com", "Foo.bar1");

        // Act - v1.0 should work
        var v1Msg = ApiTestBase.CreateAuthMessage(HttpMethod.Get, "/api/v1.0/TodoCategories", loginData.Token);
        var v1Response = await _client.SendAsync(v1Msg);
        Assert.Equal(HttpStatusCode.OK, v1Response.StatusCode);

        // Act - v2.0 should fail
        var v2Msg = ApiTestBase.CreateAuthMessage(HttpMethod.Get, "/api/v2.0/TodoCategories", loginData.Token);
        var v2Response = await _client.SendAsync(v2Msg);
        Assert.True(
            v2Response.StatusCode == HttpStatusCode.BadRequest ||
            v2Response.StatusCode == HttpStatusCode.NotFound,
            $"Expected 400 or 404, got {v2Response.StatusCode}");
    }
}
