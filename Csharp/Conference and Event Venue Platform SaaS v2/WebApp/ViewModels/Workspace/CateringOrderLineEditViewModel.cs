using System.ComponentModel.DataAnnotations;
using App.Resources.Views.Workspace;

namespace WebApp.ViewModels.Workspace;

public class CateringOrderLineEditViewModel
{
    public Guid? LineId { get; set; }

    [Required(
        ErrorMessageResourceType = typeof(Pages),
        ErrorMessageResourceName = nameof(Pages.RequiredField))]
    [Display(Name = nameof(Pages.CateringLineNameLabel), ResourceType = typeof(Pages))]
    public string Name { get; set; } = string.Empty;

    [Range(
        1,
        int.MaxValue,
        ErrorMessageResourceType = typeof(Pages),
        ErrorMessageResourceName = nameof(Pages.PositiveNumberField))]
    [Display(Name = nameof(Pages.CateringLineQuantityLabel), ResourceType = typeof(Pages))]
    public int Quantity { get; set; } = 1;

    [Range(
        0.01,
        100000,
        ErrorMessageResourceType = typeof(Pages),
        ErrorMessageResourceName = nameof(Pages.PositiveNumberField))]
    [Display(Name = nameof(Pages.CateringLinePriceLabel), ResourceType = typeof(Pages))]
    public decimal UnitPriceAmount { get; set; }

    public string Currency { get; set; } = "EUR";

    [Display(Name = nameof(Pages.CateringLineDietaryLabel), ResourceType = typeof(Pages))]
    public string? DietaryNotes { get; set; }
}
