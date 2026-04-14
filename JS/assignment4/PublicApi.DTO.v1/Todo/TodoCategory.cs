using System.ComponentModel.DataAnnotations;

namespace PublicApi.DTO.v1.Todo;

public class TodoCategory
{
    public Guid Id { get; set; }
        
    [MaxLength(128)]
    public string CategoryName { get; set; } = default!;

    public int CategorySort { get; set; }

    public DateTime SyncDt { get; set; } = DateTime.UtcNow;

    public string? Tag { get; set; }
}