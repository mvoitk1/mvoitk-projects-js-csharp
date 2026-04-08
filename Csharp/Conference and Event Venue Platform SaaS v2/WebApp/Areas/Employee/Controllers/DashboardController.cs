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
public class DashboardController(
    IVenueMembershipService venueMembershipService,
    IEmployeeWorkspaceService employeeWorkspaceService) : WorkspaceControllerBase(venueMembershipService)
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var context = await BuildWorkspaceContextAsync(cancellationToken);
        if (context.ActiveVenue == null)
        {
            return View(new EmployeeDashboardPageViewModel
            {
                Layout = new WorkspaceLayoutViewModel
                {
                    Context = context,
                    ActiveNavigation = "dashboard",
                    PageTitle = Pages.EmployeeDashboardTitle
                }
            });
        }

        var dashboard = await employeeWorkspaceService.GetDashboardAsync(User.UserId(), context.ActiveVenue.VenueId, cancellationToken);
        return View(new EmployeeDashboardPageViewModel
        {
            Layout = new WorkspaceLayoutViewModel
            {
                Context = context,
                ActiveNavigation = "dashboard",
                PageTitle = Pages.EmployeeDashboardTitle
            },
            Dashboard = dashboard
        });
    }
}
