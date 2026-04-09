
namespace PublicApi.DTO.v1.Todo;

public static class TodoCategoryExtensions
{
    public static PublicApi.DTO.v1.Todo.TodoCategory MapToDTO(this App.Domain.Todo.TodoCategory entity)
    {
        return new PublicApi.DTO.v1.Todo.TodoCategory
        {
            Id = entity.Id,
            CategoryName = entity.CategoryName,
            CategorySort = entity.CategorySort,
            SyncDt = entity.SyncDt,
            Tag = entity.Tag
        };
    }
    
    public static App.Domain.Todo.TodoCategory MapToEntity(this PublicApi.DTO.v1.Todo.TodoCategory entity)
    {
        return new App.Domain.Todo.TodoCategory
        {
            Id = entity.Id,
            CategoryName = entity.CategoryName,
            CategorySort = entity.CategorySort,
            SyncDt = entity.SyncDt,
            Tag = entity.Tag
        };
    }
    
    public static App.Domain.Todo.TodoCategory MapToEntity(this PublicApi.DTO.v1.Todo.TodoCategoryCreate entity)
    {
        return new App.Domain.Todo.TodoCategory
        {
            CategoryName = entity.CategoryName,
            CategorySort = entity.CategorySort,
            Tag = entity.Tag
        };
    }
}