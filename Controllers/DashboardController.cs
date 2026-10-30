using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartAttendance.Web.Authorization;
using SmartAttendance.Web.Infrastructure;
using SmartAttendance.Web.Services.Interfaces;

namespace SmartAttendance.Web.Controllers;

[Authorize]
public class DashboardController : BaseController
{
    private readonly IDashboardService _dashboard;

    public DashboardController(IDashboardService dashboard) => _dashboard = dashboard;

    [Authorize(Policy = PolicyNames.AdminOnly)]
    public async Task<IActionResult> Admin(CancellationToken ct) =>
        View(await _dashboard.GetAdminAsync(ct));

    [Authorize(Policy = PolicyNames.TeacherPortal)]
    public async Task<IActionResult> Teacher(CancellationToken ct) =>
        View(await _dashboard.GetTeacherAsync(User.GetUserId(), ct));
}