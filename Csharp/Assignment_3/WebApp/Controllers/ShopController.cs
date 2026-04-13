using App.BLL.Services;
using Microsoft.AspNetCore.Mvc;
using WebApp.ViewModels;

namespace WebApp.Controllers;

public class ShopController(
    IProductService productService,
    ICategoryService categoryService,
    ICollectionService collectionService) : Controller
{
    public async Task<IActionResult> Index(
        Guid? categoryId, Guid? collectionId, string? gender)
    {
        var vm = new ShopIndexViewModel
        {
            Products = await productService.GetListAsync(categoryId, collectionId, gender),
            Categories = await categoryService.GetAllAsync(),
            Collections = await collectionService.GetActiveAsync(),
            SelectedCategoryId = categoryId,
            SelectedCollectionId = collectionId,
            SelectedGender = gender
        };
        return View(vm);
    }

    public async Task<IActionResult> Detail(Guid id)
    {
        var product = await productService.GetByIdAsync(id);
        if (product == null) return NotFound();
        return View(product);
    }
}
