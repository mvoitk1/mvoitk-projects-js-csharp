using App.BLL.Services;
using App.DTO.v1.Venues.Public;
using App.Resources.Views.Public;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.Helpers;
using WebApp.ViewModels.Public;

namespace WebApp.Controllers;

public class VenuesController(IPublicVenueDiscoveryService publicVenueDiscoveryService) : Controller
{
    [HttpGet("/venues")]
    [AllowAnonymous]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var venues = await publicVenueDiscoveryService.GetBrowseVenuesAsync(cancellationToken);
        return View("Browse", venues.ToBrowseViewModel());
    }

    [HttpGet("/venues/{slug}")]
    [AllowAnonymous]
    public async Task<IActionResult> Details(string slug, CancellationToken cancellationToken)
    {
        var venue = await publicVenueDiscoveryService.GetVenueAsync(slug, cancellationToken);
        if (venue == null)
        {
            return NotFound();
        }

        return View(venue.ToDetailsViewModel());
    }

    [HttpGet("/venues/request")]
    [Authorize]
    public IActionResult RequestVenue()
    {
        var model = new VenueAccessRequestViewModel
        {
            ContactEmail = User.Identity?.Name ?? string.Empty
        };

        return View(model);
    }

    [HttpPost("/venues/request")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RequestVenue(VenueAccessRequestViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var result = await publicVenueDiscoveryService.SubmitVenueAccessRequestAsync(
                User.UserId(),
                new SubmitVenueAccessRequestDto
                {
                    CompanyName = model.CompanyName,
                    VenueName = model.VenueName,
                    ContactName = model.ContactName,
                    ContactEmail = model.ContactEmail,
                    ContactPhone = model.ContactPhone,
                    City = model.City,
                    Country = model.Country,
                    AddressLine1 = model.AddressLine1,
                    EstimatedMonthlyBookings = model.EstimatedMonthlyBookings,
                    Notes = model.Notes
                },
                cancellationToken);

            TempData["VenueRequestSuccess"] = string.Format(
                Pages.RequestSuccessBody,
                result.RequestId);

            return RedirectToAction(nameof(RequestVenue));
        }
        catch (ArgumentException)
        {
            ModelState.AddModelError(string.Empty, Pages.RequiredField);
        }

        return View(model);
    }
}
