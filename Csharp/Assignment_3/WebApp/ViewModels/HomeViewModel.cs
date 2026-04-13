using App.DTO.v1.Categories;
using App.DTO.v1.Collections;
using App.DTO.v1.Products;

namespace WebApp.ViewModels;

public class HomeViewModel
{
    public IEnumerable<ProductListItemDto> FeaturedProducts { get; set; } = [];
    public IEnumerable<CategoryDto> Categories { get; set; } = [];
    public IEnumerable<CollectionDto> Collections { get; set; } = [];
}
