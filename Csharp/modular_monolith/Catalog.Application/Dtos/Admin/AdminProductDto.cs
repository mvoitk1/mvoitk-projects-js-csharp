namespace Catalog.Application.Dtos.Admin;

public class AdminProductDto
{
    public Guid Id { get; set; }
    public string NameEn { get; set; } = string.Empty;
    public string NameEt { get; set; } = string.Empty;
    public string DescriptionEn { get; set; } = string.Empty;
    public string DescriptionEt { get; set; } = string.Empty;
    public string MaterialEn { get; set; } = string.Empty;
    public string MaterialEt { get; set; } = string.Empty;
    public string Gender { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid? CollectionId { get; set; }
    public string? CollectionName { get; set; }
    public List<AdminVariantDto> Variants { get; set; } = new();
    public List<AdminProductImageDto> Images { get; set; } = new();
    public List<Guid> CategoryIds { get; set; } = new();
}
