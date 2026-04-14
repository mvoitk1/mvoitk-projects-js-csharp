using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using App.DAL.EF;
using App.DTO.v1.Categories;
using App.DTO.v1.Collections;
using App.DTO.v1.Products;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WebApp.ViewModels;

namespace WebApp.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly AppDbContext _context;

    public HomeController(AppDbContext context, ILogger<HomeController> logger)
    {
        _logger = logger;
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var collections = await _context.Collections
            .Where(c => c.IsActive)
            .OrderByDescending(c => c.LaunchDate)
            .Take(3)
            .Select(c => new CollectionDto
            {
                Id = c.Id,
                Name = c.Name.Translate()!,
                Description = c.Description.Translate()!,
                LaunchDate = c.LaunchDate,
                IsActive = c.IsActive
            })
            .ToListAsync();

        var categories = await _context.Categories
            .Where(c => c.ParentCategoryId == null)
            .Take(4)
            .Select(c => new CategoryDto
            {
                Id = c.Id,
                Name = c.Name.Translate()!,
                ParentCategoryId = c.ParentCategoryId
            })
            .ToListAsync();

        var products = await _context.Products
            .Where(p => p.IsActive)
            .Include(p => p.Variants)
            .Include(p => p.Images)
            .OrderByDescending(p => p.CreatedAt)
            .Take(6)
            .ToListAsync();

        var featuredProducts = products.Select(p => new ProductListItemDto
        {
            Id = p.Id,
            Name = p.Name.Translate()!,
            Gender = p.Gender.ToString(),
            IsActive = p.IsActive,
            LowestPrice = p.Variants.Any() ? p.Variants.Min(v => v.Price) : null,
            PrimaryImageUrl = p.Images.OrderBy(i => i.SortOrder).FirstOrDefault()?.Url
        }).ToList();

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