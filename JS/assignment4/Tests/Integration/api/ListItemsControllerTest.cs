using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using App.DAL.EF.Seeding;
using Microsoft.AspNetCore.Mvc.Testing;
using Tests.Helpers;
using Tests.Integration;
using WebApp;

namespace App.Test.Integration.api;

[Collection("NonParallel")]
public class ListItemsControllerTest : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory<Program> _factory;
    private readonly Guid _apiKey1 = InitialData.ApiKey1Secret;
    private readonly Guid _apiKey2 = InitialData.ApiKey2Secret;

    public ListItemsControllerTest(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task GetWithoutApiKey_Returns400()
    {
        // Act - GET without apiKey parameter (uses empty Guid default)
        var response = await _client.GetAsync($"/api/v1.0/ListItems?apiKey={Guid.Empty}");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var contentStr = await response.Content.ReadAsStringAsync();
        Assert.Contains("Problem with ApiKey", contentStr);
    }

    [Fact]
    public async Task GetWithInvalidApiKey_Returns400()
    {
        // Act
        var response = await _client.GetAsync($"/api/v1.0/ListItems?apiKey={Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var contentStr = await response.Content.ReadAsStringAsync();
        Assert.Contains("Problem with ApiKey", contentStr);
    }

    [Fact]
    public async Task CrudLifecycle_CreateReadUpdateListDelete()
    {
        // Create
        var item = TestDataFactory.CreateListItem("CrudItem", false);
        var createResponse = await _client.PostAsJsonAsync($"/api/v1.0/ListItems?apiKey={_apiKey1}", item);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = JsonSerializer.Deserialize<JsonElement>(await createResponse.Content.ReadAsStringAsync(),
            JsonHelper.CamelCase);
        var createdId = created.GetProperty("id").GetGuid();
        Assert.NotEqual(Guid.Empty, createdId);
        Assert.Equal("CrudItem", created.GetProperty("description").GetString());

        // Read
        var getResponse = await _client.GetAsync($"/api/v1.0/ListItems/{createdId}?apiKey={_apiKey1}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        // Update (toggle completed)
        var putBody = new { id = createdId, description = "CrudItem", completed = true };
        var putResponse =
            await _client.PutAsJsonAsync($"/api/v1.0/ListItems/{createdId}?apiKey={_apiKey1}", putBody);
        Assert.Equal(HttpStatusCode.NoContent, putResponse.StatusCode);

        // GET list (verify updated)
        var listResponse = await _client.GetAsync($"/api/v1.0/ListItems?apiKey={_apiKey1}");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        var list = JsonSerializer.Deserialize<List<JsonElement>>(
            await listResponse.Content.ReadAsStringAsync(), JsonHelper.CamelCase);
        Assert.NotNull(list);
        var updatedItem = list.FirstOrDefault(i => i.GetProperty("id").GetGuid() == createdId);
        Assert.True(updatedItem.GetProperty("completed").GetBoolean());

        // Delete
        var deleteResponse = await _client.DeleteAsync($"/api/v1.0/ListItems/{createdId}?apiKey={_apiKey1}");
        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);
        var deletedItem = JsonSerializer.Deserialize<JsonElement>(
            await deleteResponse.Content.ReadAsStringAsync(), JsonHelper.CamelCase);
        Assert.Equal(createdId, deletedItem.GetProperty("id").GetGuid());

        // Verify deleted
        var getDeletedResponse =
            await _client.GetAsync($"/api/v1.0/ListItems/{createdId}?apiKey={_apiKey1}");
        Assert.Equal(HttpStatusCode.NotFound, getDeletedResponse.StatusCode);
    }

    [Fact]
    public async Task GetList_CompletedFilter()
    {
        // Create one completed + one non-completed item
        var completedItem = TestDataFactory.CreateListItem("CompletedItem", true);
        var notCompletedItem = TestDataFactory.CreateListItem("NotCompletedItem", false);

        await _client.PostAsJsonAsync($"/api/v1.0/ListItems?apiKey={_apiKey1}", completedItem);
        await _client.PostAsJsonAsync($"/api/v1.0/ListItems?apiKey={_apiKey1}", notCompletedItem);

        // GET completed=true
        var completedResponse =
            await _client.GetAsync($"/api/v1.0/ListItems?apiKey={_apiKey1}&completed=true");
        var completedList = JsonSerializer.Deserialize<List<JsonElement>>(
            await completedResponse.Content.ReadAsStringAsync(), JsonHelper.CamelCase);
        Assert.NotNull(completedList);
        Assert.All(completedList, i => Assert.True(i.GetProperty("completed").GetBoolean()));

        // GET completed=false
        var notCompletedResponse =
            await _client.GetAsync($"/api/v1.0/ListItems?apiKey={_apiKey1}&completed=false");
        var notCompletedList = JsonSerializer.Deserialize<List<JsonElement>>(
            await notCompletedResponse.Content.ReadAsStringAsync(), JsonHelper.CamelCase);
        Assert.NotNull(notCompletedList);
        Assert.All(notCompletedList, i => Assert.False(i.GetProperty("completed").GetBoolean()));

        // GET without filter (returns both)
        var allResponse = await _client.GetAsync($"/api/v1.0/ListItems?apiKey={_apiKey1}");
        var allList = JsonSerializer.Deserialize<List<JsonElement>>(
            await allResponse.Content.ReadAsStringAsync(), JsonHelper.CamelCase);
        Assert.NotNull(allList);
        Assert.True(allList.Count >= 2);
    }

    [Fact]
    public async Task CrossKeyIsolation_KeyBCannotSeeKeyAItems()
    {
        // Create item under key 1
        var item = TestDataFactory.CreateListItem("IsolatedItem");
        var createResponse = await _client.PostAsJsonAsync($"/api/v1.0/ListItems?apiKey={_apiKey1}", item);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = JsonSerializer.Deserialize<JsonElement>(
            await createResponse.Content.ReadAsStringAsync(), JsonHelper.CamelCase);
        var createdId = created.GetProperty("id").GetGuid();

        // GET list with key 2 (should not see key 1's items)
        var listResponse = await _client.GetAsync($"/api/v1.0/ListItems?apiKey={_apiKey2}");
        var list = JsonSerializer.Deserialize<List<JsonElement>>(
            await listResponse.Content.ReadAsStringAsync(), JsonHelper.CamelCase);
        Assert.NotNull(list);
        Assert.DoesNotContain(list, i => i.GetProperty("id").GetGuid() == createdId);

        // GET item by ID with key 2 (should return 404)
        var getResponse = await _client.GetAsync($"/api/v1.0/ListItems/{createdId}?apiKey={_apiKey2}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task PutMismatchedId_Returns400()
    {
        // Create an item
        var item = TestDataFactory.CreateListItem("MismatchItem");
        var createResponse = await _client.PostAsJsonAsync($"/api/v1.0/ListItems?apiKey={_apiKey1}", item);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = JsonSerializer.Deserialize<JsonElement>(
            await createResponse.Content.ReadAsStringAsync(), JsonHelper.CamelCase);
        var createdId = created.GetProperty("id").GetGuid();

        // PUT with mismatched ID
        var mismatchedId = Guid.NewGuid();
        var putBody = new { id = createdId, description = "MismatchItem", completed = false };
        var putResponse =
            await _client.PutAsJsonAsync($"/api/v1.0/ListItems/{mismatchedId}?apiKey={_apiKey1}", putBody);

        Assert.Equal(HttpStatusCode.BadRequest, putResponse.StatusCode);
    }
}
