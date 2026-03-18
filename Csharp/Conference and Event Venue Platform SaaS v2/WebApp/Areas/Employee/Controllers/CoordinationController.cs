using App.BLL.Services;
using App.Domain.Identity;
using App.Resources.Views.Workspace;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.Helpers;
using WebApp.Infrastructure;
using WebApp.ViewModels.Workspace;

namespace WebApp.Areas.Employee.Controllers;

[Area("Employee")]
[Authorize(Roles = $"{AppRoles.CompanyEmployee},{AppRoles.CompanyManager}")]
public class CoordinationController(
    IVenueMembershipService venueMembershipService,
    IEmployeeWorkspaceService employeeWorkspaceService) : WorkspaceControllerBase(venueMembershipService)
{
    public async Task<IActionResult> Index(Guid? bookingId, CancellationToken cancellationToken)
    {
        var context = await BuildWorkspaceContextAsync(cancellationToken);
        var bookings = context.ActiveVenue == null
            ? []
            : await employeeWorkspaceService.GetBookingsAsync(User.UserId(), context.ActiveVenue.VenueId, cancellationToken);

        var selectedBookingId = bookingId ?? bookings.FirstOrDefault()?.BookingId;
        var coordination = selectedBookingId.HasValue && context.ActiveVenue != null
            ? await employeeWorkspaceService.GetCoordinationAsync(User.UserId(), context.ActiveVenue.VenueId, selectedBookingId.Value, cancellationToken)
            : null;

        return View(new EmployeeCoordinationPageViewModel
        {
            Layout = new WorkspaceLayoutViewModel
            {
                Context = context,
                ActiveNavigation = "coordination",
                PageTitle = Pages.EmployeeCoordinationTitle
            },
            AvailableBookings = bookings,
            Coordination = coordination
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(Guid bookingId, CancellationToken cancellationToken)
    {
        var context = await BuildWorkspaceContextAsync(cancellationToken);
        if (context.ActiveVenue == null)
        {
            TempData["WorkspaceError"] = Pages.VenueRequiredMessage;
            return RedirectToAction(nameof(Index), new { bookingId });
        }

        try
        {
            await employeeWorkspaceService.ApproveBookingRequestAsync(
                User.UserId(),
                context.ActiveVenue.VenueId,
                bookingId,
                cancellationToken);

            TempData["WorkspaceSuccess"] = Pages.BookingApproveSuccess;
        }
        catch (Exception ex) when (ex is InvalidOperationException or KeyNotFoundException)
        {
            TempData["WorkspaceError"] = ex is InvalidOperationException ? ex.Message : Pages.BookingApproveFailed;
        }

        return RedirectToAction(nameof(Index), new { bookingId });
    }
}
