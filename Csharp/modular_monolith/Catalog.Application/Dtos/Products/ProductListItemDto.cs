namespace Catalog.Application.Dtos.Products;

public class ProductListItemDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Gender { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public decimal? LowestPrice { get; set; }
    public string? PrimaryImageUrl { get; set; }
    public string? CollectionName { get; set; }
}
