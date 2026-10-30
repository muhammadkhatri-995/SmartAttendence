using Microsoft.AspNetCore.Mvc;

namespace SmartAttendance.Web.Controllers;

public abstract class BaseController : Controller
{
    protected void SetSuccess(string message) => TempData["Success"] = message;
    protected void SetError(string message) => TempData["Error"] = message;
    protected void SetWarning(string message) => TempData["Warning"] = message;
}