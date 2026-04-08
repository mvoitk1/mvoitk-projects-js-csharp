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
public class BookingsController(
    IVenueMembershipService venueMembershipService,
    IEmployeeWorkspaceService employeeWorkspaceService) : WorkspaceControllerBase(venueMembershipService)
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var context = await BuildWorkspaceContextAsync(cancellationToken);
        var bookings = context.ActiveVenue == null
            ? []
            : await employeeWorkspaceService.GetBookingsAsync(User.UserId(), context.ActiveVenue.VenueId, cancellationToken);

        return View(new EmployeeBookingsPageViewModel
        {
            Layout = new WorkspaceLayoutViewModel
            {
                Context = context,
                ActiveNavigation = "bookings",
                PageTitle = Pages.EmployeeBookingsTitle
            },
            Bookings = bookings,
            BookingCalendar = new BookingCalendarSectionViewModel
            {
                Title = Pages.EmployeeBookingsCalendarTitle,
                Description = Pages.EmployeeBookingsCalendarDescription,
                EmptyMessage = Pages.EmployeeBookingsEmpty,
                Bookings = bookings
                    .OrderBy(item => item.Schedule.StartsAt)
                    .Select(item => item.ToVenueCalendarItemViewModel())
                    .ToList()
            }
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
            return RedirectToAction(nameof(Index));
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

        return RedirectToAction(nameof(Index));
    }
}
