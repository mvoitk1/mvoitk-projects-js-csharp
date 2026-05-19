namespace Catalog.Web.Dtos.v1.Admin;

public class AdminCollectionDto
{
    public Guid Id { get; set; }
    public string NameEn { get; set; } = string.Empty;
    public string NameEt { get; set; } = string.Empty;
    public string DescriptionEn { get; set; } = string.Empty;
    public string DescriptionEt { get; set; } = string.Empty;
    public DateTime? LaunchDate { get; set; }
    public bool IsActive { get; set; }
}
