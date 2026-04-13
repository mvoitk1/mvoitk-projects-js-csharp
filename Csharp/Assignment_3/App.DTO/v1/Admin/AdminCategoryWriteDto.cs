using System.ComponentModel.DataAnnotations;

namespace App.DTO.v1.Admin;

public class AdminCategoryWriteDto
{
    [Required]
    public string NameEn { get; set; } = string.Empty;
    public string NameEt { get; set; } = string.Empty;
    public Guid? ParentCategoryId { get; set; }
}
