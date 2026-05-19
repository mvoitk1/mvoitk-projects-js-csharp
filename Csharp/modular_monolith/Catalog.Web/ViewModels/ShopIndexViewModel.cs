using Catalog.Web.Dtos.v1.Categories;
using Catalog.Web.Dtos.v1.Collections;
using Catalog.Web.Dtos.v1.Products;

namespace Catalog.Web.ViewModels;

public class ShopIndexViewModel
{
    public IEnumerable<ProductListItemDto> Products { get; set; } = [];
    public IEnumerable<CategoryDto> Categories { get; set; } = [];
    public IEnumerable<CollectionDto> Collections { get; set; } = [];

    public Guid? SelectedCategoryId { get; set; }
    public Guid? SelectedCollectionId { get; set; }
    public string? SelectedGender { get; set; }
}
