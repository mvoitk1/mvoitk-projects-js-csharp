using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using PublicApi.DTO.v1.Identity;
using PublicApi.DTO.v1.Todo;
using Tests.Helpers;
using Tests.Integration;
using WebApp;

namespace App.Test.Integration.api;

[Collection("NonParallel")]
public class TodoCategoriesControllerTest : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory<Program> _factory;

    public TodoCategoriesControllerTest(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task IndexRequiresLogin()
    {
        // Act
        var response = await _client.GetAsync("/api/v1.0/TodoCategories");
        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task IndexWithUser()
    {
        var user = "akaver@akaver.com";
        var pass = "Foo.bar1";

        // get jwt
        var response =
            await _client.PostAsJsonAsync("/api/v1.0/Account/Login", new {email = user, password = pass});
        var contentStr = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();

        var loginData = JsonSerializer.Deserialize<JwtResponse>(contentStr, JsonHelper.CamelCase);

        Assert.NotNull(loginData);
        Assert.NotNull(loginData.Token);
        Assert.True(loginData.Token.Length > 0);

        var msg = new HttpRequestMessage(HttpMethod.Get, "/api/v1.0/TodoCategories");
        msg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", loginData.Token);
        msg.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        response = await _client.SendAsync(msg);

        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task CrudLifecycle_CreateReadUpdateDelete()
    {
        // Arrange
        var email = $"test-{Guid.NewGuid():N}@test.com";
        var jwt = await ApiTestBase.RegisterAsync(_client, email, "Test.pass1", "Crud", "Cat");
        var category = TestDataFactory.CreateTodoCategory("TestCat", 5);

        // Create
        var createMsg = ApiTestBase.CreateAuthMessage(HttpMethod.Post, "/api/v1.0/TodoCategories", jwt.Token);
        createMsg.Content = JsonContent.Create(category);
        var createResponse = await _client.SendAsync(createMsg);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = JsonSerializer.Deserialize<TodoCategory>(
            await createResponse.Content.ReadAsStringAsync(), JsonHelper.CamelCase);
        Assert.NotNull(created);
        Assert.NotEqual(Guid.Empty, created.Id);

        // Read
        var getMsg = ApiTestBase.CreateAuthMessage(HttpMethod.Get,
            $"/api/v1.0/TodoCategories/{created.Id}", jwt.Token);
        var getResponse = await _client.SendAsync(getMsg);
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var fetched = JsonSerializer.Deserialize<TodoCategory>(
            await getResponse.Content.ReadAsStringAsync(), JsonHelper.CamelCase);
        Assert.NotNull(fetched);
        Assert.Equal("TestCat", fetched.CategoryName);
        Assert.Equal(5, fetched.CategorySort);

        // Update
        fetched.CategoryName = "UpdatedCat";
        fetched.CategorySort = 10;
        var putMsg = ApiTestBase.CreateAuthMessage(HttpMethod.Put,
            $"/api/v1.0/TodoCategories/{created.Id}", jwt.Token);
        putMsg.Content = JsonContent.Create(fetched);
        var putResponse = await _client.SendAsync(putMsg);
        Assert.Equal(HttpStatusCode.OK, putResponse.StatusCode);
        var updated = JsonSerializer.Deserialize<TodoCategory>(
            await putResponse.Content.ReadAsStringAsync(), JsonHelper.CamelCase);
        Assert.NotNull(updated);
        Assert.Equal("UpdatedCat", updated.CategoryName);
        Assert.Equal(10, updated.CategorySort);

        // Delete
        var deleteMsg = ApiTestBase.CreateAuthMessage(HttpMethod.Delete,
            $"/api/v1.0/TodoCategories/{created.Id}", jwt.Token);
        var deleteResponse = await _client.SendAsync(deleteMsg);
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        // Verify deleted
        var getDeletedMsg = ApiTestBase.CreateAuthMessage(HttpMethod.Get,
            $"/api/v1.0/TodoCategories/{created.Id}", jwt.Token);
        var getDeletedResponse = await _client.SendAsync(getDeletedMsg);
        Assert.Equal(HttpStatusCode.NotFound, getDeletedResponse.StatusCode);
    }

    [Fact]
    public async Task UserIsolation_EachUserSeesOnlyOwnCategories()
    {
        // Register two users
        var emailA = $"test-{Guid.NewGuid():N}@test.com";
        var emailB = $"test-{Guid.NewGuid():N}@test.com";
        var jwtA = await ApiTestBase.RegisterAsync(_client, emailA, "Test.pass1", "UserA", "Iso");
        var jwtB = await ApiTestBase.RegisterAsync(_client, emailB, "Test.pass1", "UserB", "Iso");

        // Each creates a category
        var catA = TestDataFactory.CreateTodoCategory("CatA");
        var createMsgA = ApiTestBase.CreateAuthMessage(HttpMethod.Post, "/api/v1.0/TodoCategories", jwtA.Token);
        createMsgA.Content = JsonContent.Create(catA);
        await _client.SendAsync(createMsgA);

        var catB = TestDataFactory.CreateTodoCategory("CatB");
        var createMsgB = ApiTestBase.CreateAuthMessage(HttpMethod.Post, "/api/v1.0/TodoCategories", jwtB.Token);
        createMsgB.Content = JsonContent.Create(catB);
        await _client.SendAsync(createMsgB);

        // User A gets list
        var listMsgA = ApiTestBase.CreateAuthMessage(HttpMethod.Get, "/api/v1.0/TodoCategories", jwtA.Token);
        var listResponseA = await _client.SendAsync(listMsgA);
        var listA = JsonSerializer.Deserialize<List<TodoCategory>>(
            await listResponseA.Content.ReadAsStringAsync(), JsonHelper.CamelCase);
        Assert.NotNull(listA);
        Assert.All(listA, c => Assert.Equal("CatA", c.CategoryName));

        // User B gets list
        var listMsgB = ApiTestBase.CreateAuthMessage(HttpMethod.Get, "/api/v1.0/TodoCategories", jwtB.Token);
        var listResponseB = await _client.SendAsync(listMsgB);
        var listB = JsonSerializer.Deserialize<List<TodoCategory>>(
            await listResponseB.Content.ReadAsStringAsync(), JsonHelper.CamelCase);
        Assert.NotNull(listB);
        Assert.All(listB, c => Assert.Equal("CatB", c.CategoryName));
    }

    [Fact]
    public async Task PutMismatchedId_Returns400()
    {
        // Arrange
        var email = $"test-{Guid.NewGuid():N}@test.com";
        var jwt = await ApiTestBase.RegisterAsync(_client, email, "Test.pass1", "Mis", "Match");

        var category = TestDataFactory.CreateTodoCategory("MismatchCat");
        var createMsg = ApiTestBase.CreateAuthMessage(HttpMethod.Post, "/api/v1.0/TodoCategories", jwt.Token);
        createMsg.Content = JsonContent.Create(category);
        var createResponse = await _client.SendAsync(createMsg);
        var created = JsonSerializer.Deserialize<TodoCategory>(
            await createResponse.Content.ReadAsStringAsync(), JsonHelper.CamelCase);
        Assert.NotNull(created);

        // Act - PUT with mismatched ID
        var mismatchedId = Guid.NewGuid();
        var putMsg = ApiTestBase.CreateAuthMessage(HttpMethod.Put,
            $"/api/v1.0/TodoCategories/{mismatchedId}", jwt.Token);
        putMsg.Content = JsonContent.Create(created);
        var putResponse = await _client.SendAsync(putMsg);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, putResponse.StatusCode);
    }

    [Fact]
    public async Task DeleteOtherUsersCategory_Returns404()
    {
        // User A creates a category
        var emailA = $"test-{Guid.NewGuid():N}@test.com";
        var jwtA = await ApiTestBase.RegisterAsync(_client, emailA, "Test.pass1", "Del", "Owner");

        var category = TestDataFactory.CreateTodoCategory("DelCat");
        var createMsg = ApiTestBase.CreateAuthMessage(HttpMethod.Post, "/api/v1.0/TodoCategories", jwtA.Token);
        createMsg.Content = JsonContent.Create(category);
        var createResponse = await _client.SendAsync(createMsg);
        var created = JsonSerializer.Deserialize<TodoCategory>(
            await createResponse.Content.ReadAsStringAsync(), JsonHelper.CamelCase);
        Assert.NotNull(created);

        // User B tries to delete User A's category
        var emailB = $"test-{Guid.NewGuid():N}@test.com";
        var jwtB = await ApiTestBase.RegisterAsync(_client, emailB, "Test.pass1", "Del", "Other");

        var deleteMsg = ApiTestBase.CreateAuthMessage(HttpMethod.Delete,
            $"/api/v1.0/TodoCategories/{created.Id}", jwtB.Token);
        var deleteResponse = await _client.SendAsync(deleteMsg);

        Assert.Equal(HttpStatusCode.NotFound, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task GetList_SortedByCategorySortThenName()
    {
        // Arrange
        var email = $"test-{Guid.NewGuid():N}@test.com";
        var jwt = await ApiTestBase.RegisterAsync(_client, email, "Test.pass1", "Sort", "Test");

        // Create 3 categories: (Zebra, sort=1), (Alpha, sort=2), (Beta, sort=1)
        var categories = new[]
        {
            TestDataFactory.CreateTodoCategory("Zebra", 1),
            TestDataFactory.CreateTodoCategory("Alpha", 2),
            TestDataFactory.CreateTodoCategory("Beta", 1),
        };

        foreach (var cat in categories)
        {
            var msg = ApiTestBase.CreateAuthMessage(HttpMethod.Post, "/api/v1.0/TodoCategories", jwt.Token);
            msg.Content = JsonContent.Create(cat);
            await _client.SendAsync(msg);
        }

        // Act
        var listMsg = ApiTestBase.CreateAuthMessage(HttpMethod.Get, "/api/v1.0/TodoCategories", jwt.Token);
        var listResponse = await _client.SendAsync(listMsg);
        var list = JsonSerializer.Deserialize<List<TodoCategory>>(
            await listResponse.Content.ReadAsStringAsync(), JsonHelper.CamelCase);

        // Assert - expected order: Beta (sort=1), Zebra (sort=1), Alpha (sort=2)
        Assert.NotNull(list);
        Assert.Equal(3, list.Count);
        Assert.Equal("Beta", list[0].CategoryName);
        Assert.Equal("Zebra", list[1].CategoryName);
        Assert.Equal("Alpha", list[2].CategoryName);
    }

    [Fact]
    public async Task DeleteWithDependentTasks_Returns409Conflict()
    {
        // Arrange - create user, category, priority, and a task using that category
        var email = $"test-{Guid.NewGuid():N}@test.com";
        var jwt = await ApiTestBase.RegisterAsync(_client, email, "Test.pass1", "Del", "Dep");

        // Create a category
        var category = TestDataFactory.CreateTodoCategory("DepCat");
        var createCatMsg = ApiTestBase.CreateAuthMessage(HttpMethod.Post, "/api/v1.0/TodoCategories", jwt.Token);
        createCatMsg.Content = JsonContent.Create(category);
        var createCatResponse = await _client.SendAsync(createCatMsg);
        Assert.Equal(HttpStatusCode.Created, createCatResponse.StatusCode);
        var createdCat = JsonSerializer.Deserialize<TodoCategory>(
            await createCatResponse.Content.ReadAsStringAsync(), JsonHelper.CamelCase);
        Assert.NotNull(createdCat);

        // Create a priority (needed for the task)
        var priority = TestDataFactory.CreateTodoPriority("DepPri");
        var createPriMsg = ApiTestBase.CreateAuthMessage(HttpMethod.Post, "/api/v1.0/TodoPriorities", jwt.Token);
        createPriMsg.Content = JsonContent.Create(priority);
        var createPriResponse = await _client.SendAsync(createPriMsg);
        Assert.Equal(HttpStatusCode.Created, createPriResponse.StatusCode);
        var createdPri = JsonSerializer.Deserialize<TodoPriority>(
            await createPriResponse.Content.ReadAsStringAsync(), JsonHelper.CamelCase);
        Assert.NotNull(createdPri);

        // Create a task that depends on the category
        var task = TestDataFactory.CreateTodoTask("DepTask", createdCat.Id, createdPri.Id);
        var createTaskMsg = ApiTestBase.CreateAuthMessage(HttpMethod.Post, "/api/v1.0/TodoTasks", jwt.Token);
        createTaskMsg.Content = JsonContent.Create(task);
        var createTaskResponse = await _client.SendAsync(createTaskMsg);
        Assert.Equal(HttpStatusCode.Created, createTaskResponse.StatusCode);

        // Act - attempt to delete the category
        var deleteMsg = ApiTestBase.CreateAuthMessage(HttpMethod.Delete,
            $"/api/v1.0/TodoCategories/{createdCat.Id}", jwt.Token);
        var deleteResponse = await _client.SendAsync(deleteMsg);

        // Assert - should return 409 Conflict
        Assert.Equal(HttpStatusCode.Conflict, deleteResponse.StatusCode);
        var body = await deleteResponse.Content.ReadAsStringAsync();
        Assert.Contains("1", body);
        Assert.Contains("dependent", body, StringComparison.OrdinalIgnoreCase);
    }
}