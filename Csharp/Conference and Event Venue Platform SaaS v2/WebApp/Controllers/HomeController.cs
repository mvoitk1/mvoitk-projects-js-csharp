using System;
using System.Diagnostics;
using System.Threading.Tasks;
using App.BLL.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using WebApp.ViewModels;
using WebApp.ViewModels.Public;

namespace WebApp.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IPublicVenueDiscoveryService _publicVenueDiscoveryService;
    private static int _counter = 0;

    public HomeController(
        IPublicVenueDiscoveryService publicVenueDiscoveryService,
        ILogger<HomeController> logger)
    {
        _logger = logger;
        _publicVenueDiscoveryService = publicVenueDiscoveryService;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var landingPage = await _publicVenueDiscoveryService.GetLandingPageAsync(cancellationToken);
        return View(landingPage.ToViewModel());
    }

    public async Task<string> HtmxClicked()
    {
        _counter++;
        return "Htmx Click Me - " + _counter;
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
