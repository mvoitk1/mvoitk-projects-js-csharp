using App.BLL.Services;
using App.Domain.Identity;
using App.DTO.v1.Venues.Employee;
using App.Resources.Views.Workspace;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.Helpers;
using WebApp.Infrastructure;
using WebApp.ViewModels.Workspace;

namespace WebApp.Areas.Employee.Controllers;

[Area("Employee")]
[Authorize(Roles = $"{AppRoles.CompanyEmployee},{AppRoles.CompanyManager}")]
public class CateringController(
    IVenueMembershipService venueMembershipService,
    IEmployeeWorkspaceService employeeWorkspaceService) : WorkspaceControllerBase(venueMembershipService)
{
    public async Task<IActionResult> Index(Guid? orderId, CancellationToken cancellationToken)
    {
        var context = await BuildWorkspaceContextAsync(cancellationToken);
        var orders = context.ActiveVenue == null
            ? []
            : await employeeWorkspaceService.GetCateringOrdersAsync(User.UserId(), context.ActiveVenue.VenueId, cancellationToken);

        var selectedOrder = orderId.HasValue
            ? orders.FirstOrDefault(item => item.CateringOrderId == orderId.Value)
            : orders.FirstOrDefault();

        return View(new CateringOrdersPageViewModel
        {
            Layout = new WorkspaceLayoutViewModel
            {
                Context = context,
                ActiveNavigation = "catering",
                PageTitle = Pages.EmployeeCateringTitle
            },
            Orders = orders,
            SelectedOrder = selectedOrder,
            EditForm = selectedOrder?.ToEditViewModel() ?? new CateringOrderEditViewModel()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(CateringOrderEditViewModel form, CancellationToken cancellationToken)
    {
        var context = await BuildWorkspaceContextAsync(cancellationToken);
        if (context.ActiveVenue == null)
        {
            TempData["WorkspaceError"] = Pages.VenueRequiredMessage;
            return RedirectToAction(nameof(Index));
        }

        if (!ModelState.IsValid)
        {
            return await BuildIndexResultAsync(context, form.CateringOrderId, form, cancellationToken);
        }

        try
        {
            await employeeWorkspaceService.UpdateCateringOrderAsync(
                User.UserId(),
                context.ActiveVenue.VenueId,
                form.CateringOrderId,
                new CateringOrderEditDto
                {
                    GuestCount = form.GuestCount,
                    Notes = form.Notes,
                    Lines = form.Lines
                        .Where(line => !string.IsNullOrWhiteSpace(line.Name))
                        .Select(line => new CateringOrderLineEditDto
                        {
                            LineId = line.LineId,
                            Name = line.Name,
                            Quantity = line.Quantity,
                            UnitPriceAmount = line.UnitPriceAmount,
                            Currency = line.Currency,
                            DietaryNotes = line.DietaryNotes
                        })
                        .ToList()
                },
                cancellationToken);

            TempData["WorkspaceSuccess"] = Pages.CateringSaveSuccess;
            return RedirectToAction(nameof(Index), new { orderId = form.CateringOrderId });
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException or ArgumentOutOfRangeException)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return await BuildIndexResultAsync(context, form.CateringOrderId, form, cancellationToken);
        }
    }

    private async Task<ViewResult> BuildIndexResultAsync(
        WorkspaceContextViewModel context,
        Guid? orderId,
        CateringOrderEditViewModel editForm,
        CancellationToken cancellationToken)
    {
        var orders = await employeeWorkspaceService.GetCateringOrdersAsync(User.UserId(), RequireActiveVenue(context).VenueId, cancellationToken);
        var selectedOrder = orderId.HasValue
            ? orders.FirstOrDefault(item => item.CateringOrderId == orderId.Value)
            : orders.FirstOrDefault();

        return View("Index", new CateringOrdersPageViewModel
        {
            Layout = new WorkspaceLayoutViewModel
            {
                Context = context,
                ActiveNavigation = "catering",
                PageTitle = Pages.EmployeeCateringTitle
            },
            Orders = orders,
            SelectedOrder = selectedOrder,
            EditForm = editForm
        });
    }
}
