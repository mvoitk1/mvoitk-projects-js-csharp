using Catalog.Web.Dtos.v1.Admin;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Catalog.Web.Areas.Admin.ViewModels;

public class CategoryFormViewModel
{
    public AdminCategoryWriteDto Form { get; init; } = new();
    public SelectList ParentOptions { get; init; } = new(Array.Empty<object>());
}
