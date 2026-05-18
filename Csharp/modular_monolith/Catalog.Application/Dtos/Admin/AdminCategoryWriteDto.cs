using System.ComponentModel.DataAnnotations;

namespace Catalog.Application.Dtos.Admin;

public class AdminCategoryWriteDto
{
    [Required]
    public string NameEn { get; set; } = string.Empty;
    public string NameEt { get; set; } = string.Empty;
    public Guid? ParentCategoryId { get; set; }
}
