using App.BLL.Services;
using App.Domain.Identity;
using App.Domain.Venues;
using App.Resources.Views.Workspace;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.Helpers;
using WebApp.Infrastructure;
using WebApp.ViewModels.Workspace;

namespace WebApp.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = AppRoles.CompanyManager)]
public class DashboardController(
    IVenueMembershipService venueMembershipService,
    IVenueAdminService venueAdminService,
    IEmployeeWorkspaceService employeeWorkspaceService) : WorkspaceControllerBase(venueMembershipService)
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var context = await BuildWorkspaceContextAsync(cancellationToken);
        if (context.ActiveVenue != null &&
            Enum.TryParse<VenueAccessLevel>(context.ActiveVenue.AccessLevel, true, out var accessLevel) &&
            accessLevel != VenueAccessLevel.Manager)
        {
            return RedirectToAction("Index", "Dashboard", new { area = "Employee" });
        }

        if (context.ActiveVenue == null)
        {
            return View(new AdminDashboardPageViewModel
            {
                Layout = new WorkspaceLayoutViewModel
                {
                    Context = context,
                    ActiveNavigation = "dashboard",
                    PageTitle = Pages.ManagerDashboardTitle
                }
            });
        }

        var dashboard = await venueAdminService.GetDashboardAsync(User.UserId(), context.ActiveVenue.VenueId, cancellationToken);
        var bookings = await employeeWorkspaceService.GetBookingsAsync(User.UserId(), context.ActiveVenue.VenueId, cancellationToken);
        return View(new AdminDashboardPageViewModel
        {
            Layout = new WorkspaceLayoutViewModel
            {
                Context = context,
                ActiveNavigation = "dashboard",
                PageTitle = Pages.ManagerDashboardTitle
            },
            Dashboard = dashboard,
            BookingCalendar = new BookingCalendarSectionViewModel
            {
                Title = Pages.ManagerBookingsCalendarTitle,
                Description = Pages.ManagerBookingsCalendarDescription,
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
