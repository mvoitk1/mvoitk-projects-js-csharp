using System.Diagnostics;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using WebApp.ViewModels;

namespace WebApp.Controllers;

public class SiteController(ILogger<SiteController> logger) : Controller
{
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
                new CookieOptions
                {
                    Expires = DateTimeOffset.UtcNow.AddYears(1),
                }
            );
        }
        catch (Exception e)
        {
            logger.LogError("SetLanguage exception: {}", e.Message);
        }

        return LocalRedirect(returnUrl);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
