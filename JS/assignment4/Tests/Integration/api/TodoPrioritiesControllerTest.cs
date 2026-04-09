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
public class TodoPrioritiesControllerTest : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory<Program> _factory;

    public TodoPrioritiesControllerTest(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task GetWithoutAuth_Returns401()
    {
        // Act
        var response = await _client.GetAsync("/api/v1.0/TodoPriorities");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CrudLifecycle_CreateReadUpdateDelete()
    {
        // Arrange
        var email = $"test-{Guid.NewGuid():N}@test.com";
        var jwt = await ApiTestBase.RegisterAsync(_client, email, "Test.pass1", "Crud", "Pri");
        var priority = TestDataFactory.CreateTodoPriority("TestPri", 5);

        // Create
        var createMsg = ApiTestBase.CreateAuthMessage(HttpMethod.Post, "/api/v1.0/TodoPriorities", jwt.Token);
        createMsg.Content = JsonContent.Create(priority);
        var createResponse = await _client.SendAsync(createMsg);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = JsonSerializer.Deserialize<TodoPriority>(
            await createResponse.Content.ReadAsStringAsync(), JsonHelper.CamelCase);
        Assert.NotNull(created);
        Assert.NotEqual(Guid.Empty, created.Id);

        // Read
        var getMsg = ApiTestBase.CreateAuthMessage(HttpMethod.Get,
            $"/api/v1.0/TodoPriorities/{created.Id}", jwt.Token);
        var getResponse = await _client.SendAsync(getMsg);
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var fetched = JsonSerializer.Deserialize<TodoPriority>(
            await getResponse.Content.ReadAsStringAsync(), JsonHelper.CamelCase);
        Assert.NotNull(fetched);
        Assert.Equal("TestPri", fetched.PriorityName);
        Assert.Equal(5, fetched.PrioritySort);

        // Update
        fetched.PriorityName = "UpdatedPri";
        fetched.PrioritySort = 10;
        var putMsg = ApiTestBase.CreateAuthMessage(HttpMethod.Put,
            $"/api/v1.0/TodoPriorities/{created.Id}", jwt.Token);
        putMsg.Content = JsonContent.Create(fetched);
        var putResponse = await _client.SendAsync(putMsg);
        Assert.Equal(HttpStatusCode.OK, putResponse.StatusCode);

        // Delete
        var deleteMsg = ApiTestBase.CreateAuthMessage(HttpMethod.Delete,
            $"/api/v1.0/TodoPriorities/{created.Id}", jwt.Token);
        var deleteResponse = await _client.SendAsync(deleteMsg);
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        // Verify deleted
        var getDeletedMsg = ApiTestBase.CreateAuthMessage(HttpMethod.Get,
            $"/api/v1.0/TodoPriorities/{created.Id}", jwt.Token);
        var getDeletedResponse = await _client.SendAsync(getDeletedMsg);
        Assert.Equal(HttpStatusCode.NotFound, getDeletedResponse.StatusCode);
    }

    [Fact]
    public async Task GetList_FilteredByUserAndSorted()
    {
        // Arrange - User A creates 3 priorities
        var emailA = $"test-{Guid.NewGuid():N}@test.com";
        var jwtA = await ApiTestBase.RegisterAsync(_client, emailA, "Test.pass1", "Sort", "PriA");

        var priorities = new[]
        {
            TestDataFactory.CreateTodoPriority("Zebra", 1),
            TestDataFactory.CreateTodoPriority("Alpha", 2),
            TestDataFactory.CreateTodoPriority("Beta", 1),
        };

        foreach (var pri in priorities)
        {
            var msg = ApiTestBase.CreateAuthMessage(HttpMethod.Post, "/api/v1.0/TodoPriorities", jwtA.Token);
            msg.Content = JsonContent.Create(pri);
            await _client.SendAsync(msg);
        }

        // Act - User A gets list
        var listMsgA = ApiTestBase.CreateAuthMessage(HttpMethod.Get, "/api/v1.0/TodoPriorities", jwtA.Token);
        var listResponseA = await _client.SendAsync(listMsgA);
        var listA = JsonSerializer.Deserialize<List<TodoPriority>>(
            await listResponseA.Content.ReadAsStringAsync(), JsonHelper.CamelCase);

        // Assert - sorted by PrioritySort then PriorityName: Beta(1), Zebra(1), Alpha(2)
        Assert.NotNull(listA);
        Assert.Equal(3, listA.Count);
        Assert.Equal("Beta", listA[0].PriorityName);
        Assert.Equal("Zebra", listA[1].PriorityName);
        Assert.Equal("Alpha", listA[2].PriorityName);

        // User B registers and gets empty list
        var emailB = $"test-{Guid.NewGuid():N}@test.com";
        var jwtB = await ApiTestBase.RegisterAsync(_client, emailB, "Test.pass1", "Sort", "PriB");
        var listMsgB = ApiTestBase.CreateAuthMessage(HttpMethod.Get, "/api/v1.0/TodoPriorities", jwtB.Token);
        var listResponseB = await _client.SendAsync(listMsgB);
        var listB = JsonSerializer.Deserialize<List<TodoPriority>>(
            await listResponseB.Content.ReadAsStringAsync(), JsonHelper.CamelCase);
        Assert.NotNull(listB);
        Assert.Empty(listB);
    }

    [Fact]
    public async Task UserIsolation_CrossUserAccessDenied()
    {
        // User A creates a priority
        var emailA = $"test-{Guid.NewGuid():N}@test.com";
        var jwtA = await ApiTestBase.RegisterAsync(_client, emailA, "Test.pass1", "Iso", "PriA");

        var priority = TestDataFactory.CreateTodoPriority("IsoPri");
        var createMsg = ApiTestBase.CreateAuthMessage(HttpMethod.Post, "/api/v1.0/TodoPriorities", jwtA.Token);
        createMsg.Content = JsonContent.Create(priority);
        var createResponse = await _client.SendAsync(createMsg);
        var created = JsonSerializer.Deserialize<TodoPriority>(
            await createResponse.Content.ReadAsStringAsync(), JsonHelper.CamelCase);
        Assert.NotNull(created);

        // User B tries to access User A's priority
        var emailB = $"test-{Guid.NewGuid():N}@test.com";
        var jwtB = await ApiTestBase.RegisterAsync(_client, emailB, "Test.pass1", "Iso", "PriB");

        // GET by ID
        var getMsg = ApiTestBase.CreateAuthMessage(HttpMethod.Get,
            $"/api/v1.0/TodoPriorities/{created.Id}", jwtB.Token);
        var getResponse = await _client.SendAsync(getMsg);
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);

        // DELETE
        var deleteMsg = ApiTestBase.CreateAuthMessage(HttpMethod.Delete,
            $"/api/v1.0/TodoPriorities/{created.Id}", jwtB.Token);
        var deleteResponse = await _client.SendAsync(deleteMsg);
        Assert.Equal(HttpStatusCode.NotFound, deleteResponse.StatusCode);

        // NOTE: Known IDOR vulnerability on PUT endpoint at TodoPrioritiesController.cs:66
        // The PUT endpoint uses EntityState.Modified without verifying the record belongs
        // to the current user, allowing any authenticated user to overwrite another user's
        // priority record. This is a known issue documented as a regression baseline.
    }

    [Fact]
    public async Task DeleteWithDependentTasks_Returns409Conflict()
    {
        // Arrange - create user, priority, category, and a task using that priority
        var email = $"test-{Guid.NewGuid():N}@test.com";
        var jwt = await ApiTestBase.RegisterAsync(_client, email, "Test.pass1", "Del", "Dep");

        // Create a priority
        var priority = TestDataFactory.CreateTodoPriority("DepPri");
        var createPriMsg = ApiTestBase.CreateAuthMessage(HttpMethod.Post, "/api/v1.0/TodoPriorities", jwt.Token);
        createPriMsg.Content = JsonContent.Create(priority);
        var createPriResponse = await _client.SendAsync(createPriMsg);
        Assert.Equal(HttpStatusCode.Created, createPriResponse.StatusCode);
        var createdPri = JsonSerializer.Deserialize<TodoPriority>(
            await createPriResponse.Content.ReadAsStringAsync(), JsonHelper.CamelCase);
        Assert.NotNull(createdPri);

        // Create a category (needed for the task)
        var category = TestDataFactory.CreateTodoCategory("DepCat");
        var createCatMsg = ApiTestBase.CreateAuthMessage(HttpMethod.Post, "/api/v1.0/TodoCategories", jwt.Token);
        createCatMsg.Content = JsonContent.Create(category);
        var createCatResponse = await _client.SendAsync(createCatMsg);
        Assert.Equal(HttpStatusCode.Created, createCatResponse.StatusCode);
        var createdCat = JsonSerializer.Deserialize<TodoCategory>(
            await createCatResponse.Content.ReadAsStringAsync(), JsonHelper.CamelCase);
        Assert.NotNull(createdCat);

        // Create a task that depends on the priority
        var task = TestDataFactory.CreateTodoTask("DepTask", createdCat.Id, createdPri.Id);
        var createTaskMsg = ApiTestBase.CreateAuthMessage(HttpMethod.Post, "/api/v1.0/TodoTasks", jwt.Token);
        createTaskMsg.Content = JsonContent.Create(task);
        var createTaskResponse = await _client.SendAsync(createTaskMsg);
        Assert.Equal(HttpStatusCode.Created, createTaskResponse.StatusCode);

        // Act - attempt to delete the priority
        var deleteMsg = ApiTestBase.CreateAuthMessage(HttpMethod.Delete,
            $"/api/v1.0/TodoPriorities/{createdPri.Id}", jwt.Token);
        var deleteResponse = await _client.SendAsync(deleteMsg);

        // Assert - should return 409 Conflict
        Assert.Equal(HttpStatusCode.Conflict, deleteResponse.StatusCode);
        var body = await deleteResponse.Content.ReadAsStringAsync();
        Assert.Contains("1", body);
        Assert.Contains("dependent", body, StringComparison.OrdinalIgnoreCase);
    }
}
