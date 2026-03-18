using System.ComponentModel.DataAnnotations;
using App.Resources.Views.Public;

namespace WebApp.ViewModels.Public;

public class PublicBookingRequestViewModel
{
    [Required(
        ErrorMessageResourceType = typeof(Pages),
        ErrorMessageResourceName = nameof(Pages.RequiredField))]
    [Display(Name = nameof(Pages.SpaceSelectionLabel), ResourceType = typeof(Pages))]
    public Guid? SpaceId { get; set; }

    [Display(Name = nameof(Pages.LayoutSelectionLabel), ResourceType = typeof(Pages))]
    public Guid? LayoutId { get; set; }

    [Required(
        ErrorMessageResourceType = typeof(Pages),
        ErrorMessageResourceName = nameof(Pages.RequiredField))]
    [Display(Name = nameof(Pages.EventTitleLabel), ResourceType = typeof(Pages))]
    public string EventTitle { get; set; } = string.Empty;

    [Required(
        ErrorMessageResourceType = typeof(Pages),
        ErrorMessageResourceName = nameof(Pages.RequiredField))]
    [Display(Name = nameof(Pages.ClientNameLabel), ResourceType = typeof(Pages))]
    public string ClientName { get; set; } = string.Empty;

    [DisplayFormat(DataFormatString = "{0:yyyy-MM-ddTHH:mm}", ApplyFormatInEditMode = true)]
    [DataType(DataType.DateTime)]
    [Display(Name = nameof(Pages.StartsAtLabel), ResourceType = typeof(Pages))]
    public DateTime StartsAt { get; set; } = DateTime.UtcNow.Date.AddDays(14).AddHours(9);

    [DisplayFormat(DataFormatString = "{0:yyyy-MM-ddTHH:mm}", ApplyFormatInEditMode = true)]
    [DataType(DataType.DateTime)]
    [Display(Name = nameof(Pages.EndsAtLabel), ResourceType = typeof(Pages))]
    public DateTime EndsAt { get; set; } = DateTime.UtcNow.Date.AddDays(14).AddHours(17);

    [Range(
        1,
        5000,
        ErrorMessageResourceType = typeof(Pages),
        ErrorMessageResourceName = nameof(Pages.PositiveAttendeesRange))]
    [Display(Name = nameof(Pages.ExpectedAttendeesLabel), ResourceType = typeof(Pages))]
    public int ExpectedAttendees { get; set; } = 24;

    [Display(Name = nameof(Pages.CateringNotesLabel), ResourceType = typeof(Pages))]
    public string? CateringNotes { get; set; }

    [Display(Name = nameof(Pages.SetupRequirementsLabel), ResourceType = typeof(Pages))]
    public string? SetupRequirements { get; set; }

    [Display(Name = nameof(Pages.AdditionalRequirementsLabel), ResourceType = typeof(Pages))]
    public string? AdditionalRequirements { get; set; }
}
