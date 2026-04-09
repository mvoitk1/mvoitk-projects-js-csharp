namespace PublicApi.DTO.v1.Todo;

public static class TodoTaskExtensions
{
    public static PublicApi.DTO.v1.Todo.TodoTask MapToDTO(this App.Domain.Todo.TodoTask entity)
    {
        return new PublicApi.DTO.v1.Todo.TodoTask
        {
            Id = entity.Id,
            TaskName = entity.TaskName,
            TaskSort= entity.TaskSort,
            CreatedDt= entity.CreatedDt,
            DueDt= entity.DueDt,
            IsCompleted= entity.IsCompleted,
            IsArchived= entity.IsArchived,
            TodoCategoryId= entity.TodoCategoryId,
            TodoPriorityId= entity.TodoPriorityId,
            SyncDt = entity.SyncDt,
        };
    }

    
    public static App.Domain.Todo.TodoTask MapToEntity(this PublicApi.DTO.v1.Todo.TodoTask entity)
    {
        return new App.Domain.Todo.TodoTask
        {
            Id = entity.Id,
            TaskName = entity.TaskName,
            TaskSort= entity.TaskSort,
            CreatedDt= entity.CreatedDt,
            DueDt= entity.DueDt,
            IsCompleted= entity.IsCompleted,
            IsArchived= entity.IsArchived,
            TodoCategoryId= entity.TodoCategoryId,
            TodoPriorityId= entity.TodoPriorityId,
            SyncDt = entity.SyncDt,
        };
    }
    
    public static App.Domain.Todo.TodoTask MapToEntity(this PublicApi.DTO.v1.Todo.TodoTaskCreate entity)
    {
        return new App.Domain.Todo.TodoTask
        {
            TaskName = entity.TaskName,
            TaskSort= entity.TaskSort,
            CreatedDt= entity.CreatedDt,
            DueDt= entity.DueDt,
            IsCompleted= entity.IsCompleted,
            IsArchived= entity.IsArchived,
            TodoCategoryId= entity.TodoCategoryId,
            TodoPriorityId= entity.TodoPriorityId,
        };
    }
}