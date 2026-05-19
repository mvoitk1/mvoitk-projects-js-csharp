using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Users.Domain;
using Users.Web.Areas.Root.ViewModels;

namespace Users.Web.Areas.Root.Controllers;

[Area("Root")]
[Authorize(Roles = "root")]
public class UsersController(UserManager<AppUser> userManager) : Controller
{
    public async Task<IActionResult> Index()
    {
        var res = await userManager.Users.OrderBy(u => u.Email).ToListAsync();
        return View(res);
    }

    public async Task<IActionResult> RoleRemove(Guid userId, string role)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            return RedirectToAction("Index", new { error = "User Not Found" });
        }

        var result = await userManager.RemoveFromRoleAsync(user, role);
        if (result.Succeeded)
        {
            return RedirectToAction("Index");
        }

        return RedirectToAction("Index", new { error = result.Errors.Select(e => e.Description).First() });
    }

    public async Task<IActionResult> RoleAdd(Guid userId, string role)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            return RedirectToAction("Index", new { error = "User Not Found" });
        }

        var result = await userManager.AddToRoleAsync(user, role);
        if (result.Succeeded)
        {
            return RedirectToAction("Index");
        }

        return RedirectToAction("Index", new { error = result.Errors.Select(e => e.Description).First() });
    }

    public async Task<IActionResult> PasswordLink(Guid id)
    {
        var user = await userManager.FindByIdAsync(id.ToString());
        if (user == null)
        {
            return RedirectToAction("Index", new { error = "User Not Found" });
        }

        var code = await userManager.GeneratePasswordResetTokenAsync(user);

        code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
        var callbackUrl = Url.Page(
            "/Account/ResetPassword",
            pageHandler: null,
            values: new { area = "Identity", code },
            protocol: Request.Scheme);


        var url = HtmlEncoder.Default.Encode(callbackUrl!);

        var vm = new PasswordLinkViewModel
        {
            UserId = user.Id,
            UserEmail = user.Email ?? string.Empty,
            PasswordLink = url,
        };

        return View(vm);
    }
}
