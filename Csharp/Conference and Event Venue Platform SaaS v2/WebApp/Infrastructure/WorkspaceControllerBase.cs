using App.BLL.Services;
using App.DTO.v1.Venues.Membership;
using Microsoft.AspNetCore.Mvc;
using WebApp.Helpers;
using WebApp.ViewModels.Workspace;

namespace WebApp.Infrastructure;

public abstract class WorkspaceControllerBase(IVenueMembershipService venueMembershipService) : Controller
{
    protected async Task<WorkspaceContextViewModel> BuildWorkspaceContextAsync(CancellationToken cancellationToken = default)
    {
        var userId = User.UserId();
        var activeVenue = await venueMembershipService.GetActiveVenueAsync(userId, cancellationToken);
        var venueOptions = await venueMembershipService.GetVenueOptionsAsync(userId, cancellationToken);

        if (activeVenue == null)
        {
            var firstSelectable = venueOptions.FirstOrDefault(option => option.CanSelect);
            if (firstSelectable != null)
            {
                activeVenue = await venueMembershipService.SetActiveVenueAsync(userId, firstSelectable.VenueId, cancellationToken);
                venueOptions = await venueMembershipService.GetVenueOptionsAsync(userId, cancellationToken);
            }
        }

        return new WorkspaceContextViewModel
        {
            ActiveVenue = activeVenue,
            VenueOptions = venueOptions
        };
    }

    protected static ActiveVenueSelectionResultDto RequireActiveVenue(WorkspaceContextViewModel context) =>
        context.ActiveVenue ?? throw new InvalidOperationException("An active venue is required for this workspace.");
}
