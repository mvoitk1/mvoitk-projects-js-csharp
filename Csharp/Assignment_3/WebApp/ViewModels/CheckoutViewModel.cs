using System.ComponentModel.DataAnnotations;
using App.DTO.v1.Cart;

namespace WebApp.ViewModels;

public class CheckoutViewModel
{
    public CartDto Cart { get; set; } = new();

    [Required]
    [Display(Name = "First Name")]
    public string ShippingFirstName { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Last Name")]
    public string ShippingLastName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [Display(Name = "Email")]
    public string ShippingEmail { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Phone")]
    public string ShippingPhone { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Country")]
    public string ShippingCountry { get; set; } = string.Empty;

    [Required]
    [Display(Name = "City")]
    public string ShippingCity { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Street Address")]
    public string ShippingStreet { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Postal Code")]
    public string ShippingPostalCode { get; set; } = string.Empty;
}
