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

        return View(BuildVenueDetailsViewModel(venue.ToDetailsViewModel(), null));
    }

    [HttpPost("/venues/{slug}/booking-request")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RequestBooking(
        string slug,
        [Bind(Prefix = "BookingRequest")] PublicBookingRequestViewModel form,
        CancellationToken cancellationToken)
    {
        var venue = await publicVenueDiscoveryService.GetVenueAsync(slug, cancellationToken);
        if (venue == null)
        {
            return NotFound();
        }

        if (form.StartsAt >= form.EndsAt)
        {
            ModelState.AddModelError(nameof(form.EndsAt), Pages.ScheduleEndAfterStart);
        }

        if (!ModelState.IsValid)
        {
            return View("Details", BuildVenueDetailsViewModel(venue.ToDetailsViewModel(), form));
        }

        try
        {
            var result = await publicVenueDiscoveryService.SubmitBookingRequestAsync(
                User.UserId(),
                slug,
                new SubmitPublicBookingRequestDto
                {
                    SpaceId = form.SpaceId!.Value,
                    LayoutId = form.LayoutId,
                    EventTitle = form.EventTitle,
                    ClientName = form.ClientName,
                    StartsAt = form.StartsAt,
                    EndsAt = form.EndsAt,
                    ExpectedAttendees = form.ExpectedAttendees,
                    CateringNotes = form.CateringNotes,
                    SetupRequirements = form.SetupRequirements,
                    AdditionalRequirements = form.AdditionalRequirements
                },
                cancellationToken);

            TempData["BookingRequestSuccess"] = string.Format(Pages.BookingRequestSuccessBody, result.BookingId);
            return RedirectToAction(nameof(Details), new { slug });
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
        }
        catch (ArgumentException)
        {
            ModelState.AddModelError(string.Empty, Pages.RequiredField);
        }
        catch (KeyNotFoundException)
        {
            ModelState.AddModelError(string.Empty, Pages.BookingRequestUnavailable);
        }

        return View("Details", BuildVenueDetailsViewModel(venue.ToDetailsViewModel(), form));
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

    private PublicVenueDetailsViewModel BuildVenueDetailsViewModel(
        PublicVenueDetailsViewModel viewModel,
        PublicBookingRequestViewModel? form)
    {
        return new PublicVenueDetailsViewModel
        {
            VenueId = viewModel.VenueId,
            Slug = viewModel.Slug,
            Name = viewModel.Name,
            City = viewModel.City,
            Country = viewModel.Country,
            AddressLine1 = viewModel.AddressLine1,
            Description = viewModel.Description,
            HourlyRate = viewModel.HourlyRate,
            Capacity = viewModel.Capacity,
            UpcomingBookingsCount = viewModel.UpcomingBookingsCount,
            Spaces = viewModel.Spaces,
            CanRequestBooking = User.Identity?.IsAuthenticated == true,
            BookingRequest = form ?? new PublicBookingRequestViewModel
            {
                SpaceId = viewModel.Spaces.FirstOrDefault()?.SpaceId,
                LayoutId = viewModel.Spaces
                    .SelectMany(space => space.Layouts.Where(layout => layout.IsDefault))
                    .Select(layout => (Guid?) layout.LayoutId)
                    .FirstOrDefault(),
                ClientName = User.Identity?.Name ?? string.Empty
            }
        };
    }
}
