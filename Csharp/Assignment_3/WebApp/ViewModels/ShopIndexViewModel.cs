using App.DTO.v1.Categories;
using App.DTO.v1.Collections;
using App.DTO.v1.Products;

namespace WebApp.ViewModels;

public class ShopIndexViewModel
{
    public IEnumerable<ProductListItemDto> Products { get; set; } = [];
    public IEnumerable<CategoryDto> Categories { get; set; } = [];
    public IEnumerable<CollectionDto> Collections { get; set; } = [];

    // Active filters
    public Guid? SelectedCategoryId { get; set; }
    public Guid? SelectedCollectionId { get; set; }
    public string? SelectedGender { get; set; }
}
