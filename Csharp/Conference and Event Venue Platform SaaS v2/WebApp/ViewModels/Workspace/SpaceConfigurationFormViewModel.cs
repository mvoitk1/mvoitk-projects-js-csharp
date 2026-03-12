using System.ComponentModel.DataAnnotations;
using App.Resources.Views.Workspace;

namespace WebApp.ViewModels.Workspace;

public class SpaceConfigurationFormViewModel
{
    public Guid? SpaceId { get; set; }

    [Required(
        ErrorMessageResourceType = typeof(Pages),
        ErrorMessageResourceName = nameof(Pages.RequiredField))]
    [Display(Name = nameof(Pages.SpaceNameLabel), ResourceType = typeof(Pages))]
    public string Name { get; set; } = string.Empty;

    [Required(
        ErrorMessageResourceType = typeof(Pages),
        ErrorMessageResourceName = nameof(Pages.RequiredField))]
    [Display(Name = nameof(Pages.SpaceCodeLabel), ResourceType = typeof(Pages))]
    public string Code { get; set; } = string.Empty;

    [Required(
        ErrorMessageResourceType = typeof(Pages),
        ErrorMessageResourceName = nameof(Pages.RequiredField))]
    [Display(Name = nameof(Pages.SpaceStatusLabel), ResourceType = typeof(Pages))]
    public string Status { get; set; } = string.Empty;

    [Display(Name = nameof(Pages.SpaceDescriptionLabel), ResourceType = typeof(Pages))]
    public string? Description { get; set; }

    [Range(
        1,
        100000,
        ErrorMessageResourceType = typeof(Pages),
        ErrorMessageResourceName = nameof(Pages.PositiveNumberField))]
    [Display(Name = nameof(Pages.SpaceMinimumDurationLabel), ResourceType = typeof(Pages))]
    public int MinimumBookingDurationMinutes { get; set; }

    [Range(
        0.01,
        100000,
        ErrorMessageResourceType = typeof(Pages),
        ErrorMessageResourceName = nameof(Pages.PositiveNumberField))]
    [Display(Name = nameof(Pages.SpaceHourlyRateLabel), ResourceType = typeof(Pages))]
    public decimal HourlyRateAmount { get; set; }

    [Required(
        ErrorMessageResourceType = typeof(Pages),
        ErrorMessageResourceName = nameof(Pages.RequiredField))]
    [Display(Name = nameof(Pages.SpaceCurrencyLabel), ResourceType = typeof(Pages))]
    public string Currency { get; set; } = "EUR";

    [Range(
        0,
        100000,
        ErrorMessageResourceType = typeof(Pages),
        ErrorMessageResourceName = nameof(Pages.PositiveNumberField))]
    [Display(Name = nameof(Pages.SpaceMinimumCapacityLabel), ResourceType = typeof(Pages))]
    public int MinimumCapacity { get; set; }

    [Range(
        0,
        100000,
        ErrorMessageResourceType = typeof(Pages),
        ErrorMessageResourceName = nameof(Pages.PositiveNumberField))]
    [Display(Name = nameof(Pages.SpaceRecommendedCapacityLabel), ResourceType = typeof(Pages))]
    public int RecommendedCapacity { get; set; }

    [Range(
        0,
        100000,
        ErrorMessageResourceType = typeof(Pages),
        ErrorMessageResourceName = nameof(Pages.PositiveNumberField))]
    [Display(Name = nameof(Pages.SpaceMaximumCapacityLabel), ResourceType = typeof(Pages))]
    public int MaximumCapacity { get; set; }

    public List<SpaceLayoutFormViewModel> Layouts { get; set; } = [];
}
