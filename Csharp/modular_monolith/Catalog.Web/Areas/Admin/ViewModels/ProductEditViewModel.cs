using App.DTO.v1.Admin;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Catalog.Web.Areas.Admin.ViewModels;

public class ProductEditViewModel
{
    public AdminProductWriteDto Form { get; init; } = new();
    public AdminProductDto? Product { get; init; }
    public SelectList Collections { get; init; } = new(Array.Empty<object>());
    public MultiSelectList Categories { get; init; } = new(Array.Empty<object>());
    public SelectList Colors { get; init; } = new(Array.Empty<object>());
    public SelectList Sizes { get; init; } = new(Array.Empty<object>());
    public SelectList Genders { get; init; } = new(Array.Empty<object>());
}
