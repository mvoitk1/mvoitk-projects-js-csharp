using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Users.Web.Areas.Root.Controllers;

[Area("Root")]
[Authorize(Roles = "root")]
public class HomeController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
