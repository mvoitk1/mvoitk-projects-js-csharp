using System.ComponentModel.DataAnnotations;
using App.Resources.Views.Workspace;

namespace WebApp.ViewModels.Workspace;

public class SpaceLayoutFormViewModel
{
    public Guid? LayoutId { get; set; }

    [Display(Name = nameof(Pages.SpaceLayoutNameLabel), ResourceType = typeof(Pages))]
    public string? Name { get; set; }

    [Display(Name = nameof(Pages.SpaceLayoutTypeLabel), ResourceType = typeof(Pages))]
    public string? LayoutType { get; set; }

    [Range(
        0,
        100000,
        ErrorMessageResourceType = typeof(Pages),
        ErrorMessageResourceName = nameof(Pages.PositiveNumberField))]
    [Display(Name = nameof(Pages.SpaceLayoutCapacityLabel), ResourceType = typeof(Pages))]
    public int Capacity { get; set; }

    [Display(Name = nameof(Pages.SpaceLayoutDefaultLabel), ResourceType = typeof(Pages))]
    public bool IsDefault { get; set; }

    [Display(Name = nameof(Pages.SpaceLayoutNotesLabel), ResourceType = typeof(Pages))]
    public string? Notes { get; set; }
}
