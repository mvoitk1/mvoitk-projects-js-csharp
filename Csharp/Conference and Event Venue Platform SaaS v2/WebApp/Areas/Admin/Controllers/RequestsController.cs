using App.BLL.Services;
using App.Domain.Identity;
using App.DTO.v1.Venues.Admin;
using App.Resources.Views.Workspace;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.Helpers;
using WebApp.Infrastructure;
using WebApp.Services;
using WebApp.ViewModels.Workspace;

namespace WebApp.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = AppRoles.Admin)]
public class RequestsController(
    IVenueMembershipService venueMembershipService,
    IVenueAdminService venueAdminService,
    IVenueOperatorIdentityRoleSyncService venueOperatorIdentityRoleSyncService) : WorkspaceControllerBase(venueMembershipService)
{
    public async Task<IActionResult> Index(Guid? requestId, CancellationToken cancellationToken)
    {
        var context = await BuildWorkspaceContextAsync(cancellationToken);
        var requests = await venueAdminService.GetVenueAccessRequestsAsync(cancellationToken);
        var selectedRequest = await ResolveSelectedRequestAsync(requests, requestId, cancellationToken);

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
    public async Task<IActionResult> Review(
        [Bind(Prefix = nameof(VenueRequestsPageViewModel.ReviewForm))] ReviewVenueAccessRequestViewModel form,
        CancellationToken cancellationToken)
    {
        var context = await BuildWorkspaceContextAsync(cancellationToken);
        if (!ModelState.IsValid)
        {
            return await BuildIndexResultAsync(context, form.RequestId, form, cancellationToken);
        }

        try
        {
            var reviewedRequest = await venueAdminService.ReviewVenueAccessRequestAsync(
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

            await venueOperatorIdentityRoleSyncService.SyncUserRolesAsync(
                reviewedRequest.RequestorUserId,
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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ArchiveRejectedVenue(Guid requestId, CancellationToken cancellationToken)
    {
        var context = await BuildWorkspaceContextAsync(cancellationToken);

        try
        {
            await venueAdminService.ArchiveRejectedVenueAsync(User.UserId(), requestId, cancellationToken);
            TempData["WorkspaceSuccess"] = Pages.RequestArchiveRejectedVenueSuccess;
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex) when (ex is InvalidOperationException or KeyNotFoundException)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            var selectedRequestId = await ResolveFallbackSelectionAsync(requestId, cancellationToken);
            return await BuildIndexResultAsync(
                context,
                selectedRequestId,
                new ReviewVenueAccessRequestViewModel { RequestId = selectedRequestId },
                cancellationToken);
        }
    }

    private async Task<ViewResult> BuildIndexResultAsync(
        WorkspaceContextViewModel context,
        Guid requestId,
        ReviewVenueAccessRequestViewModel form,
        CancellationToken cancellationToken)
    {
        var requests = await venueAdminService.GetVenueAccessRequestsAsync(cancellationToken);
        var selectedRequest = await ResolveSelectedRequestAsync(requests, requestId, cancellationToken);

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

    private async Task<VenueAccessRequestDetailDto?> ResolveSelectedRequestAsync(
        IReadOnlyList<VenueAccessRequestListItemDto> requests,
        Guid? requestId,
        CancellationToken cancellationToken)
    {
        var selectedRequestId = requestId.HasValue && requests.Any(item => item.RequestId == requestId.Value)
            ? requestId.Value
            : requests.FirstOrDefault()?.RequestId;

        return selectedRequestId.HasValue
            ? await venueAdminService.GetVenueAccessRequestAsync(selectedRequestId.Value, cancellationToken)
            : null;
    }

    private async Task<Guid> ResolveFallbackSelectionAsync(Guid deletedOrFailedRequestId, CancellationToken cancellationToken)
    {
        var requests = await venueAdminService.GetVenueAccessRequestsAsync(cancellationToken);
        return requests.FirstOrDefault(item => item.RequestId != deletedOrFailedRequestId)?.RequestId ?? Guid.Empty;
    }
}
