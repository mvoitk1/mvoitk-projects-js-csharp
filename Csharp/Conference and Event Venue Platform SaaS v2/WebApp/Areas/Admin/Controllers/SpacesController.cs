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
[Authorize(Roles = AppRoles.CompanyManager)]
public class SpacesController(
    IVenueMembershipService venueMembershipService,
    IVenueAdminService venueAdminService) : WorkspaceControllerBase(venueMembershipService)
{
    public async Task<IActionResult> Index(Guid? spaceId, CancellationToken cancellationToken)
    {
        var context = await BuildWorkspaceContextAsync(cancellationToken);
        var spaces = context.ActiveVenue == null
            ? []
            : await venueAdminService.GetSpaceConfigurationsAsync(User.UserId(), context.ActiveVenue.VenueId, cancellationToken);

        var selectedSpace = spaceId.HasValue
            ? spaces.FirstOrDefault(item => item.SpaceId == spaceId.Value)
            : spaces.FirstOrDefault();

        return View(new SpacesPageViewModel
        {
            Layout = new WorkspaceLayoutViewModel
            {
                Context = context,
                ActiveNavigation = "spaces",
                PageTitle = Pages.SpacesTitle
            },
            Spaces = spaces,
            SelectedSpace = selectedSpace,
            Form = selectedSpace?.ToFormViewModel() ?? new SpaceConfigurationFormViewModel
            {
                Layouts = [new SpaceLayoutFormViewModel()]
            }
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(SpaceConfigurationFormViewModel form, CancellationToken cancellationToken)
    {
        var context = await BuildWorkspaceContextAsync(cancellationToken);
        if (context.ActiveVenue == null)
        {
            TempData["WorkspaceError"] = Pages.VenueRequiredMessage;
            return RedirectToAction(nameof(Index));
        }

        if (!ModelState.IsValid)
        {
            return await BuildIndexResultAsync(context, form.SpaceId, form, cancellationToken);
        }

        try
        {
            var saved = await venueAdminService.SaveSpaceConfigurationAsync(
                User.UserId(),
                context.ActiveVenue.VenueId,
                new UpsertSpaceConfigurationDto
                {
                    SpaceId = form.SpaceId,
                    Name = form.Name,
                    Code = form.Code,
                    Status = form.Status,
                    Description = form.Description,
                    MinimumBookingDurationMinutes = form.MinimumBookingDurationMinutes,
                    HourlyRateAmount = form.HourlyRateAmount,
                    Currency = form.Currency,
                    MinimumCapacity = form.MinimumCapacity,
                    RecommendedCapacity = form.RecommendedCapacity,
                    MaximumCapacity = form.MaximumCapacity,
                    Layouts = form.Layouts
                        .Where(item => !string.IsNullOrWhiteSpace(item.Name) && !string.IsNullOrWhiteSpace(item.LayoutType))
                        .Select(item => new UpsertSpaceLayoutDto
                        {
                            LayoutId = item.LayoutId,
                            Name = item.Name!,
                            LayoutType = item.LayoutType!,
                            Capacity = item.Capacity,
                            IsDefault = item.IsDefault,
                            Notes = item.Notes
                        })
                        .ToList()
                },
                cancellationToken);

            TempData["WorkspaceSuccess"] = Pages.SpaceSaveSuccess;
            return RedirectToAction(nameof(Index), new { spaceId = saved.SpaceId });
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException or ArgumentOutOfRangeException)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return await BuildIndexResultAsync(context, form.SpaceId, form, cancellationToken);
        }
    }

    private async Task<ViewResult> BuildIndexResultAsync(
        WorkspaceContextViewModel context,
        Guid? spaceId,
        SpaceConfigurationFormViewModel form,
        CancellationToken cancellationToken)
    {
        var spaces = await venueAdminService.GetSpaceConfigurationsAsync(User.UserId(), RequireActiveVenue(context).VenueId, cancellationToken);
        var selectedSpace = spaceId.HasValue
            ? spaces.FirstOrDefault(item => item.SpaceId == spaceId.Value)
            : null;

        if (form.Layouts.Count == 0)
        {
            form.Layouts.Add(new SpaceLayoutFormViewModel());
        }

        return View("Index", new SpacesPageViewModel
        {
            Layout = new WorkspaceLayoutViewModel
            {
                Context = context,
                ActiveNavigation = "spaces",
                PageTitle = Pages.SpacesTitle
            },
            Spaces = spaces,
            SelectedSpace = selectedSpace,
            Form = form
        });
    }
}
