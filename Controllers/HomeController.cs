using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SmartAttendance.Web.Controllers;

[AllowAnonymous]
public class HomeController : Controller
{
    public IActionResult Index() =>
        User.Identity?.IsAuthenticated == true
            ? RedirectToAction("Index", "Attendance")
            : RedirectToAction("Login", "Account");
}