namespace Catalog.Web.Dtos.v1.Collections;

public class CollectionDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime? LaunchDate { get; set; }
    public bool IsActive { get; set; }
}
