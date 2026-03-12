using System.ComponentModel.DataAnnotations;
using App.Resources.Views.Workspace;

namespace WebApp.ViewModels.Workspace;

public class CateringOrderEditViewModel
{
    public Guid CateringOrderId { get; set; }

    [Range(
        1,
        100000,
        ErrorMessageResourceType = typeof(Pages),
        ErrorMessageResourceName = nameof(Pages.PositiveNumberField))]
    [Display(Name = nameof(Pages.CateringGuestCountLabel), ResourceType = typeof(Pages))]
    public int GuestCount { get; set; }

    [Display(Name = nameof(Pages.CateringNotesLabel), ResourceType = typeof(Pages))]
    public string? Notes { get; set; }

    public List<CateringOrderLineEditViewModel> Lines { get; set; } = [];
}
