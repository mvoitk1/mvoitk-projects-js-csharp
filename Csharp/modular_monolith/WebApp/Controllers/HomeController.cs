using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Catalog.Application.Contracts;
using App.DTO.Mappers;
using App.DTO.v1.Categories;
using App.DTO.v1.Collections;
using App.DTO.v1.Products;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using WebApp.ViewModels;

namespace WebApp.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ICollectionService _collectionService;
    private readonly ICategoryService _categoryService;
    private readonly IProductService _productService;

    public HomeController(
        ICollectionService collectionService,
        ICategoryService categoryService,
        IProductService productService,
        ILogger<HomeController> logger)
    {
        _logger = logger;
        _collectionService = collectionService;
        _categoryService = categoryService;
        _productService = productService;
    }

    public async Task<IActionResult> Index()
    {
        var collections = (await _collectionService.GetActiveAsync()).Take(3).Cast<object>().MapList<CollectionDto>();
        var categories = (await _categoryService.GetAllAsync()).Take(4).Cast<object>().MapList<CategoryDto>();
        var featuredProducts = (await _productService.GetListAsync()).Take(6).Cast<object>().MapList<ProductListItemDto>();

        return View(new HomeViewModel
        {
            Collections = collections,
            Categories = categories,
            FeaturedProducts = featuredProducts
        });
    }

    public IActionResult Privacy()
    {
        return View();
    }

    public IActionResult SetLanguage(string culture, string returnUrl)
    {
        try
        {
            var reqCulture = new RequestCulture(culture);

            Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(reqCulture),
                new CookieOptions()
                {
                    Expires = DateTimeOffset.UtcNow.AddYears(1)
                }
            );
        }
        catch (Exception e)
        {
            _logger.LogError("SetLanguage exception: {}", e.Message);
        }

        return LocalRedirect(returnUrl);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}