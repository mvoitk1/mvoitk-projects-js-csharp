using App.DTO.v1.Admin;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace WebApp.Areas.Admin.ViewModels;

public class CategoryFormViewModel
{
    public AdminCategoryWriteDto Form { get; init; } = new();
    public SelectList ParentOptions { get; init; } = new(Array.Empty<object>());
}
