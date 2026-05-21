using System.ComponentModel.DataAnnotations;

namespace Users.Web.ViewModels;

public class RegisterViewModel
{
    [Required]
    [MaxLength(150)]
    [Display(Name = "First name")]
    public string FirstName { get; set; } = default!;

    [Required]
    [MaxLength(150)]
    [Display(Name = "Last name")]
    public string LastName { get; set; } = default!;

    [Required]
    [EmailAddress]
    [MaxLength(256)]
    public string Email { get; set; } = default!;

    [Required]
    [DataType(DataType.Password)]
    [MinLength(6)]
    [MaxLength(100)]
    public string Password { get; set; } = default!;

    [Required]
    [DataType(DataType.Password)]
    [Display(Name = "Confirm password")]
    [Compare(nameof(Password), ErrorMessage = "The password and confirmation password do not match.")]
    public string ConfirmPassword { get; set; } = default!;

    public string? ReturnUrl { get; set; }
}
