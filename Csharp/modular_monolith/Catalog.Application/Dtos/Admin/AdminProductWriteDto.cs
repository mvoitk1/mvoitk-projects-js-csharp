using System.ComponentModel.DataAnnotations;

namespace Catalog.Application.Dtos.Admin;

public class AdminProductWriteDto
{
    [Required]
    public string NameEn { get; set; } = string.Empty;
    public string NameEt { get; set; } = string.Empty;

    public string DescriptionEn { get; set; } = string.Empty;
    public string DescriptionEt { get; set; } = string.Empty;

    public string MaterialEn { get; set; } = string.Empty;
    public string MaterialEt { get; set; } = string.Empty;

    public string Gender { get; set; } = "NotSpecified";
    public bool IsActive { get; set; } = true;
    public Guid? CollectionId { get; set; }
    public List<Guid> CategoryIds { get; set; } = new();
}
