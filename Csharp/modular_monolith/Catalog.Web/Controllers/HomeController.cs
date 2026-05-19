using Catalog.Application.Contracts;
using Catalog.Web.Dtos.v1.Categories;
using Catalog.Web.Dtos.v1.Collections;
using Catalog.Web.Dtos.v1.Products;
using Catalog.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Modules.SharedKernel.Mapping;

namespace Catalog.Web.Controllers;

public class HomeController(
    ICollectionService collectionService,
    ICategoryService categoryService,
    IProductService productService) : Controller
{
    public async Task<IActionResult> Index()
    {
        var collections = (await collectionService.GetActiveAsync()).Take(3).Cast<object>().MapList<CollectionDto>();
        var categories = (await categoryService.GetAllAsync()).Take(4).Cast<object>().MapList<CategoryDto>();
        var featuredProducts = (await productService.GetListAsync()).Take(6).Cast<object>().MapList<ProductListItemDto>();

        return View(new HomeViewModel
        {
            Collections = collections,
            Categories = categories,
            FeaturedProducts = featuredProducts,
        });
    }
}
