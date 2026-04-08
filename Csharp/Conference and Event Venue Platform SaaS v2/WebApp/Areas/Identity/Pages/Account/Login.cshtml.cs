using System.ComponentModel.DataAnnotations;
using App.Domain.Identity;
using PublicPageResources = App.Resources.Views.Public.Pages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace WebApp.Areas.Identity.Pages.Account;

[AllowAnonymous]
public class LoginModel(SignInManager<AppUser> signInManager) : PageModel
{
    private const string DefaultWorkspaceUrl = "/workspace";

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
        ReturnUrl = string.IsNullOrWhiteSpace(returnUrl)
            ? DefaultWorkspaceUrl
            : returnUrl;

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var result = await signInManager.PasswordSignInAsync(
            Input.Email,
            Input.Password,
            Input.RememberMe,
            lockoutOnFailure: false);

        if (result.Succeeded)
        {
            return LocalRedirect(ReturnUrl);
        }

        ModelState.AddModelError(string.Empty, "Invalid login attempt.");
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
        [DataType(DataType.Password)]
        [Display(Name = nameof(PublicPageResources.PasswordLabel), ResourceType = typeof(PublicPageResources))]
        public string Password { get; set; } = string.Empty;

        [Display(Name = nameof(PublicPageResources.RememberMeLabel), ResourceType = typeof(PublicPageResources))]
        public bool RememberMe { get; set; }
    }
}
