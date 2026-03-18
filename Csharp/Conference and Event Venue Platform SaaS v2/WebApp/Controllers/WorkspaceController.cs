using App.BLL.Services;
using App.Domain.Identity;
using App.Domain.Venues;
using App.Resources.Views.Workspace;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.Helpers;
using WebApp.Infrastructure;
using WebApp.ViewModels.Workspace;

namespace WebApp.Controllers;

[Authorize]
public class WorkspaceController : WorkspaceControllerBase
{
    private readonly IVenueMembershipService _venueMembershipService;
    private readonly IPublicVenueDiscoveryService _publicVenueDiscoveryService;

    public WorkspaceController(
        IVenueMembershipService venueMembershipService,
        IPublicVenueDiscoveryService publicVenueDiscoveryService)
        : base(venueMembershipService)
    {
        _venueMembershipService = venueMembershipService;
        _publicVenueDiscoveryService = publicVenueDiscoveryService;
    }

    [HttpGet("/workspace")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var context = await BuildWorkspaceContextAsync(cancellationToken);
        var activeVenueAccessLevel = ParseAccessLevel(context.ActiveVenue?.AccessLevel);

        if (User.IsInRole(AppRoles.Admin))
        {
            return RedirectToAction("Index", "Requests", new { area = "Admin" });
        }

        if (activeVenueAccessLevel == VenueAccessLevel.Manager ||
            (activeVenueAccessLevel == null && User.IsInRole(AppRoles.CompanyManager)))
        {
            return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
        }

        if (activeVenueAccessLevel == VenueAccessLevel.Employee ||
            (activeVenueAccessLevel == null && User.IsInRole(AppRoles.CompanyEmployee)))
        {
            return RedirectToAction("Index", "Dashboard", new { area = "Employee" });
        }

        ViewData["Title"] = Pages.WorkspaceTitle;
        var userRequests = await _publicVenueDiscoveryService.GetUserVenueAccessRequestsAsync(User.UserId(), cancellationToken);
        var userBookingRequests = await _publicVenueDiscoveryService.GetUserBookingRequestsAsync(User.UserId(), cancellationToken);

        return View(new WorkspaceHomePageViewModel
        {
            Layout = new WorkspaceLayoutViewModel
            {
                Context = context,
                PageTitle = Pages.WorkspaceTitle
            },
            Requests = userRequests,
            BookingRequests = userBookingRequests,
            BookingCalendar = new BookingCalendarSectionViewModel
            {
                Title = Pages.WorkspaceBookingCalendarTitle,
                Description = Pages.WorkspaceBookingCalendarDescription,
                EmptyMessage = Pages.WorkspaceBookingCalendarEmpty,
                Bookings = userBookingRequests
                    .OrderBy(item => item.Schedule.StartsAt)
                    .Select(item => item.ToUserCalendarItemViewModel())
                    .ToList()
            }
        });
    }

    [HttpGet("/workspace/bookings/{bookingId:guid}")]
    public async Task<IActionResult> Booking(Guid bookingId, CancellationToken cancellationToken)
    {
        var context = await BuildWorkspaceContextAsync(cancellationToken);
        ViewData["Title"] = Pages.WorkspaceBookingDetailsTitle;

        var booking = await _publicVenueDiscoveryService.GetUserBookingRequestAsync(
            User.UserId(),
            bookingId,
            cancellationToken);

        if (booking == null)
        {
            return NotFound();
        }

        return View("BookingDetails", new WorkspaceBookingRequestDetailsPageViewModel
        {
            Layout = new WorkspaceLayoutViewModel
            {
                Context = context,
                PageTitle = Pages.WorkspaceBookingDetailsTitle
            },
            Booking = booking
        });
    }

    [HttpGet("/workspace/bookings")]
    public async Task<IActionResult> Bookings(CancellationToken cancellationToken)
    {
        var context = await BuildWorkspaceContextAsync(cancellationToken);
        ViewData["Title"] = Pages.WorkspaceUserBookingsTitle;

        var userBookingRequests = await _publicVenueDiscoveryService.GetUserBookingRequestsAsync(User.UserId(), cancellationToken);

        return View(new WorkspaceUserBookingsPageViewModel
        {
            Layout = new WorkspaceLayoutViewModel
            {
                Context = context,
                PageTitle = Pages.WorkspaceUserBookingsTitle
            },
            BookingCalendar = new BookingCalendarSectionViewModel
            {
                Title = Pages.WorkspaceBookingCalendarTitle,
                Description = Pages.WorkspaceBookingCalendarDescription,
                EmptyMessage = Pages.WorkspaceBookingCalendarEmpty,
                Bookings = userBookingRequests
                    .OrderBy(item => item.Schedule.StartsAt)
                    .Select(item => item.ToUserCalendarItemViewModel())
                    .ToList()
            }
        });
    }

    [HttpPost("/workspace/active-venue")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = $"{AppRoles.CompanyEmployee},{AppRoles.CompanyManager}")]
    public async Task<IActionResult> SetActiveVenue(Guid venueId, string? returnUrl, CancellationToken cancellationToken)
    {
        var activeVenue = await _venueMembershipService.SetActiveVenueAsync(User.UserId(), venueId, cancellationToken);

        if (!string.IsNullOrWhiteSpace(returnUrl) &&
            Url.IsLocalUrl(returnUrl) &&
            IsReturnUrlAllowedForAccessLevel(returnUrl, activeVenue.AccessLevel))
        {
            return LocalRedirect(returnUrl);
        }

        return RedirectToWorkspaceHome(activeVenue.AccessLevel);
    }

    private IActionResult RedirectToWorkspaceHome(string accessLevel)
    {
        return Enum.TryParse<VenueAccessLevel>(accessLevel, true, out var parsedAccessLevel) &&
               parsedAccessLevel == VenueAccessLevel.Manager
            ? RedirectToAction("Index", "Dashboard", new { area = "Admin" })
            : RedirectToAction("Index", "Dashboard", new { area = "Employee" });
    }

    private static bool IsReturnUrlAllowedForAccessLevel(string returnUrl, string accessLevel)
    {
        var parsedAccessLevel = ParseAccessLevel(accessLevel);
        if (parsedAccessLevel == null)
        {
            return false;
        }

        if (parsedAccessLevel == VenueAccessLevel.Manager)
        {
            return true;
        }

        return !returnUrl.StartsWith("/Admin", StringComparison.OrdinalIgnoreCase);
    }

    private static VenueAccessLevel? ParseAccessLevel(string? accessLevel)
    {
        return Enum.TryParse<VenueAccessLevel>(accessLevel, true, out var parsedAccessLevel)
            ? parsedAccessLevel
            : null;
    }
}
