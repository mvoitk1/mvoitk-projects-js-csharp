using App.BLL.Services;
using App.Domain.Identity;
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
    IVenueAdminService venueAdminService) : WorkspaceControllerBase(venueMembershipService)
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var context = await BuildWorkspaceContextAsync(cancellationToken);
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
        return View(new AdminDashboardPageViewModel
        {
            Layout = new WorkspaceLayoutViewModel
            {
                Context = context,
                ActiveNavigation = "dashboard",
                PageTitle = Pages.ManagerDashboardTitle
            },
            Dashboard = dashboard
        });
    }
}
