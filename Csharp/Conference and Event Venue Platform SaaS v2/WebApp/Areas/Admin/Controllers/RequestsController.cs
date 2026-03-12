using App.BLL.Services;
using App.Domain.Identity;
using App.DTO.v1.Venues.Admin;
using App.Resources.Views.Workspace;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.Helpers;
using WebApp.Infrastructure;
using WebApp.ViewModels.Workspace;

namespace WebApp.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = AppRoles.Admin)]
public class RequestsController(
    IVenueMembershipService venueMembershipService,
    IVenueAdminService venueAdminService) : WorkspaceControllerBase(venueMembershipService)
{
    public async Task<IActionResult> Index(Guid? requestId, CancellationToken cancellationToken)
    {
        var context = await BuildWorkspaceContextAsync(cancellationToken);
        var requests = await venueAdminService.GetVenueAccessRequestsAsync(cancellationToken);
        var selectedRequest = requestId.HasValue
            ? await venueAdminService.GetVenueAccessRequestAsync(requestId.Value, cancellationToken)
            : requests.FirstOrDefault() is { RequestId: var firstId }
                ? await venueAdminService.GetVenueAccessRequestAsync(firstId, cancellationToken)
                : null;

        return View(new VenueRequestsPageViewModel
        {
            Layout = new WorkspaceLayoutViewModel
            {
                Context = context,
                ActiveNavigation = "requests",
                PageTitle = Pages.RequestReviewTitle
            },
            Requests = requests,
            SelectedRequest = selectedRequest,
            ReviewForm = selectedRequest?.ToReviewViewModel() ?? new ReviewVenueAccessRequestViewModel()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Review(ReviewVenueAccessRequestViewModel form, CancellationToken cancellationToken)
    {
        var context = await BuildWorkspaceContextAsync(cancellationToken);
        if (!ModelState.IsValid)
        {
            return await BuildIndexResultAsync(context, form.RequestId, form, cancellationToken);
        }

        try
        {
            await venueAdminService.ReviewVenueAccessRequestAsync(
                User.UserId(),
                new ReviewVenueAccessRequestDto
                {
                    RequestId = form.RequestId,
                    Status = form.Status,
                    ReviewNotes = form.ReviewNotes,
                    ApprovedAccessLevel = form.ApprovedAccessLevel,
                    AssignMembership = form.AssignMembership
                },
                cancellationToken);

            TempData["WorkspaceSuccess"] = Pages.RequestReviewSuccess;
            return RedirectToAction(nameof(Index), new { requestId = form.RequestId });
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException or KeyNotFoundException)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return await BuildIndexResultAsync(context, form.RequestId, form, cancellationToken);
        }
    }

    private async Task<ViewResult> BuildIndexResultAsync(
        WorkspaceContextViewModel context,
        Guid requestId,
        ReviewVenueAccessRequestViewModel form,
        CancellationToken cancellationToken)
    {
        var requests = await venueAdminService.GetVenueAccessRequestsAsync(cancellationToken);
        var selectedRequest = await venueAdminService.GetVenueAccessRequestAsync(requestId, cancellationToken);

        return View("Index", new VenueRequestsPageViewModel
        {
            Layout = new WorkspaceLayoutViewModel
            {
                Context = context,
                ActiveNavigation = "requests",
                PageTitle = Pages.RequestReviewTitle
            },
            Requests = requests,
            SelectedRequest = selectedRequest,
            ReviewForm = form
        });
    }
}
