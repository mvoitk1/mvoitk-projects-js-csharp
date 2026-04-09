using PublicApi.DTO.v1.Todo;

namespace Tests.Helpers;

public static class TestDataFactory
{
    public static TodoCategoryCreate CreateTodoCategory(string? name = null, int sort = 0)
    {
        return new TodoCategoryCreate
        {
            CategoryName = name ?? $"Cat-{Guid.NewGuid().ToString()[..8]}",
            CategorySort = sort
        };
    }

    public static TodoPriorityCreate CreateTodoPriority(string? name = null, int sort = 0)
    {
        return new TodoPriorityCreate
        {
            PriorityName = name ?? $"Pri-{Guid.NewGuid().ToString()[..8]}",
            PrioritySort = sort,
            SyncDt = DateTime.UtcNow
        };
    }

    public static TodoTaskCreate CreateTodoTask(string? name = null, Guid categoryId = default,
        Guid priorityId = default)
    {
        return new TodoTaskCreate
        {
            TaskName = name ?? $"Task-{Guid.NewGuid().ToString()[..8]}",
            TaskSort = 0,
            CreatedDt = DateTime.UtcNow,
            IsCompleted = false,
            IsArchived = false,
            TodoCategoryId = categoryId,
            TodoPriorityId = priorityId
        };
    }

    public static object CreateListItem(string? description = null, bool completed = false)
    {
        return new
        {
            Description = description ?? $"Item-{Guid.NewGuid().ToString()[..8]}",
            Completed = completed
        };
    }
}
