using System.ComponentModel.DataAnnotations;
using App.Domain.Identity;
using PublicPageResources = App.Resources.Views.Public.Pages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace WebApp.Areas.Identity.Pages.Account;

[AllowAnonymous]
public class RegisterModel(
    UserManager<AppUser> userManager,
    SignInManager<AppUser> signInManager) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public void OnGet(string? returnUrl = null)
    {
        ReturnUrl = returnUrl;
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        ReturnUrl = returnUrl ?? Url.Content("~/");

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var user = new AppUser
        {
            UserName = Input.Email,
            Email = Input.Email
        };

        var result = await userManager.CreateAsync(user, Input.Password);
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(user, AppRoles.User);
            await signInManager.SignInAsync(user, isPersistent: false);
            return LocalRedirect(ReturnUrl);
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        return Page();
    }

    public class InputModel
    {
        [Required(
            ErrorMessageResourceType = typeof(PublicPageResources),
            ErrorMessageResourceName = nameof(PublicPageResources.RequiredField))]
        [EmailAddress(
            ErrorMessageResourceType = typeof(PublicPageResources),
            ErrorMessageResourceName = nameof(PublicPageResources.InvalidEmailAddress))]
        [Display(Name = nameof(PublicPageResources.EmailLabel), ResourceType = typeof(PublicPageResources))]
        public string Email { get; set; } = string.Empty;

        [Required(
            ErrorMessageResourceType = typeof(PublicPageResources),
            ErrorMessageResourceName = nameof(PublicPageResources.RequiredField))]
        [StringLength(100, MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = nameof(PublicPageResources.PasswordLabel), ResourceType = typeof(PublicPageResources))]
        public string Password { get; set; } = string.Empty;

        [Required(
            ErrorMessageResourceType = typeof(PublicPageResources),
            ErrorMessageResourceName = nameof(PublicPageResources.RequiredField))]
        [DataType(DataType.Password)]
        [Display(Name = nameof(PublicPageResources.ConfirmPasswordLabel), ResourceType = typeof(PublicPageResources))]
        [Compare(
            nameof(Password),
            ErrorMessageResourceType = typeof(PublicPageResources),
            ErrorMessageResourceName = nameof(PublicPageResources.PasswordMismatch))]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
