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
public class TodoTasksControllerTest : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory<Program> _factory;

    public TodoTasksControllerTest(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    private async Task<(TodoCategory Category, TodoPriority Priority)> CreatePrerequisitesAsync(string token)
    {
        var catCreate = TestDataFactory.CreateTodoCategory("TaskTestCat");
        var catMsg = ApiTestBase.CreateAuthMessage(HttpMethod.Post, "/api/v1.0/TodoCategories", token);
        catMsg.Content = JsonContent.Create(catCreate);
        var catResponse = await _client.SendAsync(catMsg);
        var category = JsonSerializer.Deserialize<TodoCategory>(
            await catResponse.Content.ReadAsStringAsync(), JsonHelper.CamelCase)!;

        var priCreate = TestDataFactory.CreateTodoPriority("TaskTestPri");
        var priMsg = ApiTestBase.CreateAuthMessage(HttpMethod.Post, "/api/v1.0/TodoPriorities", token);
        priMsg.Content = JsonContent.Create(priCreate);
        var priResponse = await _client.SendAsync(priMsg);
        var priority = JsonSerializer.Deserialize<TodoPriority>(
            await priResponse.Content.ReadAsStringAsync(), JsonHelper.CamelCase)!;

        return (category, priority);
    }

    [Fact]
    public async Task GetWithoutAuth_Returns401()
    {
        // Act
        var response = await _client.GetAsync("/api/v1.0/TodoTasks");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CrudLifecycle_CreateReadUpdateDelete()
    {
        // Arrange
        var email = $"test-{Guid.NewGuid():N}@test.com";
        var jwt = await ApiTestBase.RegisterAsync(_client, email, "Test.pass1", "Crud", "Task");
        var (category, priority) = await CreatePrerequisitesAsync(jwt.Token);

        var taskCreate = TestDataFactory.CreateTodoTask("TestTask", category.Id, priority.Id);

        // Create
        var createMsg = ApiTestBase.CreateAuthMessage(HttpMethod.Post, "/api/v1.0/TodoTasks", jwt.Token);
        createMsg.Content = JsonContent.Create(taskCreate);
        var createResponse = await _client.SendAsync(createMsg);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = JsonSerializer.Deserialize<TodoTask>(
            await createResponse.Content.ReadAsStringAsync(), JsonHelper.CamelCase);
        Assert.NotNull(created);
        Assert.Equal(category.Id, created.TodoCategoryId);
        Assert.Equal(priority.Id, created.TodoPriorityId);

        // Read
        var getMsg = ApiTestBase.CreateAuthMessage(HttpMethod.Get,
            $"/api/v1.0/TodoTasks/{created.Id}", jwt.Token);
        var getResponse = await _client.SendAsync(getMsg);
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        // Update
        created.TaskName = "UpdatedTask";
        var putMsg = ApiTestBase.CreateAuthMessage(HttpMethod.Put,
            $"/api/v1.0/TodoTasks/{created.Id}", jwt.Token);
        putMsg.Content = JsonContent.Create(created);
        var putResponse = await _client.SendAsync(putMsg);
        Assert.Equal(HttpStatusCode.OK, putResponse.StatusCode);

        // Delete
        var deleteMsg = ApiTestBase.CreateAuthMessage(HttpMethod.Delete,
            $"/api/v1.0/TodoTasks/{created.Id}", jwt.Token);
        var deleteResponse = await _client.SendAsync(deleteMsg);
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        // Verify deleted
        var getDeletedMsg = ApiTestBase.CreateAuthMessage(HttpMethod.Get,
            $"/api/v1.0/TodoTasks/{created.Id}", jwt.Token);
        var getDeletedResponse = await _client.SendAsync(getDeletedMsg);
        Assert.Equal(HttpStatusCode.NotFound, getDeletedResponse.StatusCode);
    }

    [Fact]
    public async Task PostWithNonExistentCategoryId_Returns400()
    {
        // Arrange
        var email = $"test-{Guid.NewGuid():N}@test.com";
        var jwt = await ApiTestBase.RegisterAsync(_client, email, "Test.pass1", "Bad", "Cat");
        var (_, priority) = await CreatePrerequisitesAsync(jwt.Token);

        var taskCreate = TestDataFactory.CreateTodoTask("BadCatTask", Guid.NewGuid(), priority.Id);

        // Act
        var createMsg = ApiTestBase.CreateAuthMessage(HttpMethod.Post, "/api/v1.0/TodoTasks", jwt.Token);
        createMsg.Content = JsonContent.Create(taskCreate);
        var response = await _client.SendAsync(createMsg);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var contentStr = await response.Content.ReadAsStringAsync();
        Assert.Contains("Could not find category or priority for current user!", contentStr);
    }

    [Fact]
    public async Task PostWithOtherUsersCategoryId_Returns400()
    {
        // User A creates category and priority
        var emailA = $"test-{Guid.NewGuid():N}@test.com";
        var jwtA = await ApiTestBase.RegisterAsync(_client, emailA, "Test.pass1", "Own", "CatA");
        var (categoryA, _) = await CreatePrerequisitesAsync(jwtA.Token);

        // User B registers and creates own priority
        var emailB = $"test-{Guid.NewGuid():N}@test.com";
        var jwtB = await ApiTestBase.RegisterAsync(_client, emailB, "Test.pass1", "Steal", "CatB");
        var priCreate = TestDataFactory.CreateTodoPriority("PriB");
        var priMsg = ApiTestBase.CreateAuthMessage(HttpMethod.Post, "/api/v1.0/TodoPriorities", jwtB.Token);
        priMsg.Content = JsonContent.Create(priCreate);
        var priResponse = await _client.SendAsync(priMsg);
        var priorityB = JsonSerializer.Deserialize<TodoPriority>(
            await priResponse.Content.ReadAsStringAsync(), JsonHelper.CamelCase)!;

        // User B tries to create task with User A's category
        var taskCreate = TestDataFactory.CreateTodoTask("StealTask", categoryA.Id, priorityB.Id);
        var createMsg = ApiTestBase.CreateAuthMessage(HttpMethod.Post, "/api/v1.0/TodoTasks", jwtB.Token);
        createMsg.Content = JsonContent.Create(taskCreate);
        var response = await _client.SendAsync(createMsg);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetList_SortedByTaskSortThenCreatedDt()
    {
        // Arrange
        var email = $"test-{Guid.NewGuid():N}@test.com";
        var jwt = await ApiTestBase.RegisterAsync(_client, email, "Test.pass1", "Sort", "Task");
        var (category, priority) = await CreatePrerequisitesAsync(jwt.Token);

        // Create 3 tasks with different sort values
        var tasks = new[]
        {
            TestDataFactory.CreateTodoTask("TaskC", category.Id, priority.Id),
            TestDataFactory.CreateTodoTask("TaskA", category.Id, priority.Id),
            TestDataFactory.CreateTodoTask("TaskB", category.Id, priority.Id),
        };
        tasks[0].TaskSort = 3;
        tasks[1].TaskSort = 1;
        tasks[2].TaskSort = 2;

        foreach (var t in tasks)
        {
            var msg = ApiTestBase.CreateAuthMessage(HttpMethod.Post, "/api/v1.0/TodoTasks", jwt.Token);
            msg.Content = JsonContent.Create(t);
            await _client.SendAsync(msg);
        }

        // Act
        var listMsg = ApiTestBase.CreateAuthMessage(HttpMethod.Get, "/api/v1.0/TodoTasks", jwt.Token);
        var listResponse = await _client.SendAsync(listMsg);
        var list = JsonSerializer.Deserialize<List<TodoTask>>(
            await listResponse.Content.ReadAsStringAsync(), JsonHelper.CamelCase);

        // Assert - ordered by TaskSort: TaskA(1), TaskB(2), TaskC(3)
        Assert.NotNull(list);
        Assert.Equal(3, list.Count);
        Assert.Equal("TaskA", list[0].TaskName);
        Assert.Equal("TaskB", list[1].TaskName);
        Assert.Equal("TaskC", list[2].TaskName);
    }

    [Fact]
    public async Task UserIsolation_CrossUserAccessDenied()
    {
        // User A creates category+priority+task
        var emailA = $"test-{Guid.NewGuid():N}@test.com";
        var jwtA = await ApiTestBase.RegisterAsync(_client, emailA, "Test.pass1", "Iso", "TaskA");
        var (category, priority) = await CreatePrerequisitesAsync(jwtA.Token);

        var taskCreate = TestDataFactory.CreateTodoTask("IsoTask", category.Id, priority.Id);
        var createMsg = ApiTestBase.CreateAuthMessage(HttpMethod.Post, "/api/v1.0/TodoTasks", jwtA.Token);
        createMsg.Content = JsonContent.Create(taskCreate);
        var createResponse = await _client.SendAsync(createMsg);
        var created = JsonSerializer.Deserialize<TodoTask>(
            await createResponse.Content.ReadAsStringAsync(), JsonHelper.CamelCase)!;

        // User B registers
        var emailB = $"test-{Guid.NewGuid():N}@test.com";
        var jwtB = await ApiTestBase.RegisterAsync(_client, emailB, "Test.pass1", "Iso", "TaskB");

        // User B gets empty list
        var listMsg = ApiTestBase.CreateAuthMessage(HttpMethod.Get, "/api/v1.0/TodoTasks", jwtB.Token);
        var listResponse = await _client.SendAsync(listMsg);
        var list = JsonSerializer.Deserialize<List<TodoTask>>(
            await listResponse.Content.ReadAsStringAsync(), JsonHelper.CamelCase);
        Assert.NotNull(list);
        Assert.Empty(list);

        // User B cannot GET by User A's task ID
        var getMsg = ApiTestBase.CreateAuthMessage(HttpMethod.Get,
            $"/api/v1.0/TodoTasks/{created.Id}", jwtB.Token);
        var getResponse = await _client.SendAsync(getMsg);
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);

        // User B cannot DELETE User A's task
        var deleteMsg = ApiTestBase.CreateAuthMessage(HttpMethod.Delete,
            $"/api/v1.0/TodoTasks/{created.Id}", jwtB.Token);
        var deleteResponse = await _client.SendAsync(deleteMsg);
        Assert.Equal(HttpStatusCode.NotFound, deleteResponse.StatusCode);
    }
}
