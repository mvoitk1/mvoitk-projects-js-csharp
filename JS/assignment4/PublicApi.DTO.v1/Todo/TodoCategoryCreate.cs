using System.ComponentModel.DataAnnotations;

namespace PublicApi.DTO.v1.Todo;

public class TodoCategoryCreate
{
    [MaxLength(128)]
    public string CategoryName { get; set; } = default!;

    public int CategorySort { get; set; }

    public string? Tag { get; set; }
}