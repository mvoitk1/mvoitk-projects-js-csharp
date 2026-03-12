using App.BLL.Services;
using App.Domain.Identity;
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

    public WorkspaceController(IVenueMembershipService venueMembershipService)
        : base(venueMembershipService)
    {
        _venueMembershipService = venueMembershipService;
    }

    [HttpGet("/workspace")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var context = await BuildWorkspaceContextAsync(cancellationToken);

        if (User.IsInRole(AppRoles.Admin))
        {
            return RedirectToAction("Index", "Requests", new { area = "Admin" });
        }

        if (User.IsInRole(AppRoles.CompanyManager))
        {
            return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
        }

        if (User.IsInRole(AppRoles.CompanyEmployee))
        {
            return RedirectToAction("Index", "Dashboard", new { area = "Employee" });
        }

        ViewData["Title"] = Pages.WorkspaceTitle;
        return View(new WorkspaceLayoutViewModel
        {
            Context = context,
            PageTitle = Pages.WorkspaceTitle
        });
    }

    [HttpPost("/workspace/active-venue")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = $"{AppRoles.CompanyEmployee},{AppRoles.CompanyManager}")]
    public async Task<IActionResult> SetActiveVenue(Guid venueId, string? returnUrl, CancellationToken cancellationToken)
    {
        await _venueMembershipService.SetActiveVenueAsync(User.UserId(), venueId, cancellationToken);

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return LocalRedirect(returnUrl);
        }

        return RedirectToAction(nameof(Index));
    }
}
