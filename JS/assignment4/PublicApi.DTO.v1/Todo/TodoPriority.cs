using System;
using System.ComponentModel.DataAnnotations;

namespace PublicApi.DTO.v1.Todo;

public class TodoPriority
{
    public Guid Id { get; set; }
        
    [MaxLength(128)]
    public string PriorityName { get; set; } = default!;

    public int PrioritySort { get; set; }
        
    public DateTime SyncDt { get; set; } = DateTime.UtcNow;
}