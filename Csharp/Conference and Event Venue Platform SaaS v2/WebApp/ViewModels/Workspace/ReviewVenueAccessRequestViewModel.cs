using System.ComponentModel.DataAnnotations;
using App.Resources.Views.Workspace;

namespace WebApp.ViewModels.Workspace;

public class ReviewVenueAccessRequestViewModel
{
    public Guid RequestId { get; set; }

    [Required(
        ErrorMessageResourceType = typeof(Pages),
        ErrorMessageResourceName = nameof(Pages.RequiredField))]
    [Display(Name = nameof(Pages.RequestReviewStatusLabel), ResourceType = typeof(Pages))]
    public string Status { get; set; } = string.Empty;

    [Display(Name = nameof(Pages.RequestReviewNotesLabel), ResourceType = typeof(Pages))]
    public string? ReviewNotes { get; set; }

    [Display(Name = nameof(Pages.RequestReviewAccessLabel), ResourceType = typeof(Pages))]
    public string? ApprovedAccessLevel { get; set; }

    [Display(Name = nameof(Pages.RequestReviewAssignLabel), ResourceType = typeof(Pages))]
    public bool AssignMembership { get; set; }
}
