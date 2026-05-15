using System.ComponentModel.DataAnnotations;

namespace App.BLL.DTO.Admin;

public class AdminCollectionWriteDto
{
    [Required]
    public string NameEn { get; set; } = string.Empty;
    public string NameEt { get; set; } = string.Empty;
    public string DescriptionEn { get; set; } = string.Empty;
    public string DescriptionEt { get; set; } = string.Empty;
    public DateTime? LaunchDate { get; set; }
    public bool IsActive { get; set; } = true;
}
