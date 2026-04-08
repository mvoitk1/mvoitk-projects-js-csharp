using System.ComponentModel.DataAnnotations;
using App.Resources.Views.Public;

namespace WebApp.ViewModels.Public;

public class VenueAccessRequestViewModel
{
    [Required(
        ErrorMessageResourceType = typeof(Pages),
        ErrorMessageResourceName = nameof(Pages.RequiredField))]
    [Display(Name = nameof(Pages.CompanyNameLabel), ResourceType = typeof(Pages))]
    public string CompanyName { get; set; } = string.Empty;

    [Required(
        ErrorMessageResourceType = typeof(Pages),
        ErrorMessageResourceName = nameof(Pages.RequiredField))]
    [Display(Name = nameof(Pages.VenueNameLabel), ResourceType = typeof(Pages))]
    public string VenueName { get; set; } = string.Empty;

    [Required(
        ErrorMessageResourceType = typeof(Pages),
        ErrorMessageResourceName = nameof(Pages.RequiredField))]
    [Display(Name = nameof(Pages.ContactNameLabel), ResourceType = typeof(Pages))]
    public string ContactName { get; set; } = string.Empty;

    [Required(
        ErrorMessageResourceType = typeof(Pages),
        ErrorMessageResourceName = nameof(Pages.RequiredField))]
    [EmailAddress(
        ErrorMessageResourceType = typeof(Pages),
        ErrorMessageResourceName = nameof(Pages.InvalidEmailAddress))]
    [Display(Name = nameof(Pages.ContactEmailLabel), ResourceType = typeof(Pages))]
    public string ContactEmail { get; set; } = string.Empty;

    [Display(Name = nameof(Pages.ContactPhoneLabel), ResourceType = typeof(Pages))]
    public string? ContactPhone { get; set; }

    [Required(
        ErrorMessageResourceType = typeof(Pages),
        ErrorMessageResourceName = nameof(Pages.RequiredField))]
    [Display(Name = nameof(Pages.CityLabel), ResourceType = typeof(Pages))]
    public string City { get; set; } = string.Empty;

    [Required(
        ErrorMessageResourceType = typeof(Pages),
        ErrorMessageResourceName = nameof(Pages.RequiredField))]
    [Display(Name = nameof(Pages.CountryLabel), ResourceType = typeof(Pages))]
    public string Country { get; set; } = string.Empty;

    [Required(
        ErrorMessageResourceType = typeof(Pages),
        ErrorMessageResourceName = nameof(Pages.RequiredField))]
    [Display(Name = nameof(Pages.AddressLabel), ResourceType = typeof(Pages))]
    public string AddressLine1 { get; set; } = string.Empty;

    [Range(
        1,
        500,
        ErrorMessageResourceType = typeof(Pages),
        ErrorMessageResourceName = nameof(Pages.MonthlyBookingsRange))]
    [Display(Name = nameof(Pages.EstimatedMonthlyBookingsLabel), ResourceType = typeof(Pages))]
    public int EstimatedMonthlyBookings { get; set; } = 12;

    [Display(Name = nameof(Pages.NotesLabel), ResourceType = typeof(Pages))]
    public string? Notes { get; set; }
}
