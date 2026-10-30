using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartAttendance.Web.ViewModels;

namespace SmartAttendance.Web.Controllers;

/// <summary>
/// Target of UseExceptionHandler("/Error") and UseStatusCodePagesWithReExecute("/Error/{0}").
/// Never shows exception details to the user; the exception is logged by the framework.
/// </summary>
[AllowAnonymous]
[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
public class ErrorController : Controller
{
    [Route("Error")]
    public IActionResult Index() => View(Build(StatusCodes.Status500InternalServerError));

    [Route("Error/{statusCode:int}")]
    public IActionResult Status(int statusCode)
    {
        if (statusCode == StatusCodes.Status403Forbidden)
            return View("~/Views/Account/AccessDenied.cshtml");

        return View("Index", Build(statusCode));
    }

    private ErrorViewModel Build(int statusCode)
    {
        var (title, message) = statusCode switch
        {
            404 => ("Page not found", "The page you are looking for does not exist or has been moved."),
            429 => ("Too many requests", "You have made too many requests. Please wait a minute and try again."),
            400 => ("Bad request", "The request could not be understood."),
            _ => ("Something went wrong", "An unexpected error occurred. Please try again. If it keeps happening, contact the administrator and quote the request ID below.")
        };

        return new ErrorViewModel
        {
            StatusCode = statusCode,
            Title = title,
            Message = message,
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
        };
    }
}