using System.ComponentModel.DataAnnotations;

namespace PublicApi.DTO.v1.Todo;

public class TodoTaskCreate
{
            
    [MaxLength(128)]
    public string TaskName { get; set; } = default!;

    public int TaskSort { get; set; }

    public DateTime CreatedDt { get; set; } = DateTime.UtcNow;
        
    public DateTime? DueDt { get; set; }
        
    public bool IsCompleted { get; set; }

    public bool IsArchived { get; set; }
        
    public Guid TodoCategoryId { get; set; }

    public Guid TodoPriorityId { get; set; }
}