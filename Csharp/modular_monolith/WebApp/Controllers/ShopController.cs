using App.BLL.Contracts;
using App.DTO.Mappers;
using App.DTO.v1.Categories;
using App.DTO.v1.Collections;
using App.DTO.v1.Products;
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
            Products = (await productService.GetListAsync(categoryId, collectionId, gender)).Cast<object>().MapList<ProductListItemDto>(),
            Categories = (await categoryService.GetAllAsync()).Cast<object>().MapList<CategoryDto>(),
            Collections = (await collectionService.GetActiveAsync()).Cast<object>().MapList<CollectionDto>(),
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
        return View(product.MapTo<ProductDto>());
    }
}
